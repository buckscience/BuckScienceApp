using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Infrastructure.Queues;
using BuckScience.Shared.Photos;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.API.Controllers;

[ApiController]
[Route("[controller]")]
public class PhotosController : ControllerBase
{
    private readonly IAppDbContext _context;
    private readonly IPhotoQueue _photoQueue;

    public PhotosController(IAppDbContext context, IPhotoQueue photoQueue)
    {
        _context = context;
        _photoQueue = photoQueue;
    }

    /// <summary>
    /// Register a photo after it has been uploaded to blob storage
    /// </summary>
    /// <param name="request">Photo registration details</param>
    /// <returns>Success response</returns>
    [HttpPost("register")]
    public async Task<IActionResult> RegisterPhoto([FromBody] RegisterPhotoRequest request)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                request.CameraId <= 0 ||
                string.IsNullOrWhiteSpace(request.ContentHash) ||
                string.IsNullOrWhiteSpace(request.ThumbBlobName) ||
                string.IsNullOrWhiteSpace(request.DisplayBlobName))
            {
                return BadRequest("Invalid request data");
            }

            // Create and save photo record using Azure pipeline constructor
            var photo = new Photo(
                request.UserId,
                request.CameraId,
                request.ContentHash,
                request.ThumbBlobName,
                request.DisplayBlobName,
                request.TakenAtUtc,
                request.Latitude,
                request.Longitude);

            _context.Photos.Add(photo);
            await _context.SaveChangesAsync();

            // Create queue message for background processing
            var queueMessage = new PhotoIngestMessage
            {
                PhotoId = photo.Id,
                UserId = request.UserId,
                CameraId = request.CameraId,
                ContentHash = request.ContentHash,
                TakenAtUtc = request.TakenAtUtc,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            // Send message to queue
            await _photoQueue.SendPhotoIngestMessageAsync(queueMessage);

            return Ok(new { PhotoId = photo.Id, Status = photo.Status });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to register photo: {ex.Message}");
        }
    }
}