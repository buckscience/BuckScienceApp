using BuckScience.Infrastructure.Queues;
using BuckScience.Shared.Photos;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.API.Controllers;

[ApiController]
[Route("[controller]")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoQueue _photoQueue;

    public PhotosController(IPhotoQueue photoQueue)
    {
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
            // TODO: Add validation
            if (string.IsNullOrWhiteSpace(request.UserId) ||
                request.CameraId <= 0 ||
                string.IsNullOrWhiteSpace(request.ContentHash) ||
                string.IsNullOrWhiteSpace(request.ThumbBlobName) ||
                string.IsNullOrWhiteSpace(request.DisplayBlobName))
            {
                return BadRequest("Invalid request data");
            }

            // TODO: Insert photo record into database
            // For now, we'll simulate the photo ID generation
            var photoId = Random.Shared.Next(1, 1000000); // This should come from database insert

            // Create queue message for background processing
            var queueMessage = new PhotoIngestMessage
            {
                PhotoId = photoId,
                UserId = request.UserId,
                CameraId = request.CameraId,
                ContentHash = request.ContentHash,
                TakenAtUtc = request.TakenAtUtc,
                Latitude = request.Latitude,
                Longitude = request.Longitude
            };

            // Send message to queue
            await _photoQueue.SendPhotoIngestMessageAsync(queueMessage);

            return Ok(new { PhotoId = photoId, Status = "processing" });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to register photo: {ex.Message}");
        }
    }
}