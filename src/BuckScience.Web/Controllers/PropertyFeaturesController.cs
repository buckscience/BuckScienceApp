using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.PropertyFeatures;
using BuckScience.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;

namespace BuckScience.Web.Controllers;

[Authorize]
[ApiController]
public class PropertyFeaturesController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly GeometryFactory _geometryFactory;
    private readonly ICurrentUserService _currentUser;

    public PropertyFeaturesController(
        IAppDbContext db,
        GeometryFactory geometryFactory,
        ICurrentUserService currentUser)
    {
        _db = db;
        _geometryFactory = geometryFactory;
        _currentUser = currentUser;
    }

    // GET /properties/{propertyId}/features
    [HttpGet]
    [Route("/properties/{propertyId:int}/features")]
    public async Task<IActionResult> GetPropertyFeatures(int propertyId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var features = await ListPropertyFeatures.HandleAsync(_db, _currentUser.Id.Value, propertyId, ct);
        return Ok(features);
    }

    // POST /properties/{propertyId}/features
    [HttpPost]
    [Route("/properties/{propertyId:int}/features")]
    public async Task<IActionResult> CreatePropertyFeature(int propertyId, [FromBody] CreatePropertyFeatureRequest request, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var cmd = new Application.PropertyFeatures.CreatePropertyFeature.Command(
                propertyId,
                request.ClassificationType,
                request.GeometryWkt,
                request.Name,
                request.Notes);

            var featureId = await Application.PropertyFeatures.CreatePropertyFeature.HandleAsync(cmd, _db, _geometryFactory, _currentUser.Id.Value, ct);
            return CreatedAtAction(nameof(GetPropertyFeature), new { featureId }, new { Id = featureId });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // GET /features/{featureId}
    [HttpGet]
    [Route("/features/{featureId:int}")]
    public async Task<IActionResult> GetPropertyFeature(int featureId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var feature = await Application.PropertyFeatures.GetPropertyFeature.HandleAsync(featureId, _db, _currentUser.Id.Value, ct);
        if (feature is null) return NotFound();

        return Ok(feature);
    }

    // PUT /features/{featureId}
    [HttpPut]
    [Route("/features/{featureId:int}")]
    public async Task<IActionResult> UpdatePropertyFeature(int featureId, [FromBody] UpdatePropertyFeatureRequest request, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var cmd = new Application.PropertyFeatures.UpdatePropertyFeature.Command(
                featureId,
                request.ClassificationType,
                request.GeometryWkt,
                request.Name,
                request.Notes);

            var success = await Application.PropertyFeatures.UpdatePropertyFeature.HandleAsync(cmd, _db, _geometryFactory, _currentUser.Id.Value, ct);
            if (!success) return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE /features/{featureId}
    [HttpDelete]
    [Route("/features/{featureId:int}")]
    public async Task<IActionResult> DeletePropertyFeature(int featureId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var success = await Application.PropertyFeatures.DeletePropertyFeature.HandleAsync(featureId, _db, _currentUser.Id.Value, ct);
        if (!success) return NotFound();

        return NoContent();
    }

    public class CreatePropertyFeatureRequest
    {
        public ClassificationType ClassificationType { get; set; }
        public string GeometryWkt { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdatePropertyFeatureRequest
    {
        public ClassificationType ClassificationType { get; set; }
        public string GeometryWkt { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Notes { get; set; }
    }
}