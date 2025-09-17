using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Tags;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/tags")]
public class TagsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TagsController(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpPost("photos/add")]
    public async Task<IActionResult> AddTagToPhotos([FromBody] AddTagToPhotosRequest request, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var command = new ManagePhotoTags.AddTagToPhotosCommand(request.PhotoIds, request.TagName);
            await ManagePhotoTags.AddTagToPhotosAsync(command, _db, ct);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("photos/remove")]
    public async Task<IActionResult> RemoveTagFromPhotos([FromBody] RemoveTagFromPhotosRequest request, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var command = new ManagePhotoTags.RemoveTagFromPhotosCommand(request.PhotoIds, request.TagId);
            await ManagePhotoTags.RemoveTagFromPhotosAsync(command, _db, ct);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("photos/{photoId:int}")]
    public async Task<IActionResult> GetPhotoTags(int photoId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var tags = await ManagePhotoTags.GetPhotoTagsAsync(photoId, _db, ct);
        return Ok(tags);
    }

    [HttpGet("properties/{propertyId:int}")]
    public async Task<IActionResult> GetPropertyTags(int propertyId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var tags = await ManagePhotoTags.GetAvailableTagsForPropertyAsync(propertyId, _db, ct);
        return Ok(tags);
    }

    public record AddTagToPhotosRequest(List<int> PhotoIds, string TagName);
    public record RemoveTagFromPhotosRequest(List<int> PhotoIds, int TagId);
}