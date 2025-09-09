using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.FeatureWeights;
using BuckScience.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.Web.Controllers;

[Authorize]
[ApiController]
public class FeatureWeightsController : ControllerBase
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public FeatureWeightsController(
        IAppDbContext db,
        ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // GET /feature-weights
    [HttpGet]
    [Route("/feature-weights")]
    public async Task<IActionResult> GetUserFeatureWeights([FromQuery] Season? currentSeason, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var featureWeights = await GetFeatureWeights.HandleAsync(_db, _currentUser.Id.Value, currentSeason, ct);
        return Ok(featureWeights);
    }

    // PUT /feature-weights
    [HttpPut]
    [Route("/feature-weights")]
    public async Task<IActionResult> UpdateUserFeatureWeights([FromBody] UpdateFeatureWeightsRequest request, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var featureWeights = request.FeatureWeights.ToDictionary(
                kvp => kvp.Key,
                kvp => new UpdateFeatureWeights.FeatureWeightUpdate(
                    kvp.Value.UserWeight,
                    kvp.Value.SeasonalWeights));

            var command = new UpdateFeatureWeights.Command(featureWeights);
            var success = await UpdateFeatureWeights.HandleAsync(command, _db, _currentUser.Id.Value, ct);
            
            if (!success) return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public class UpdateFeatureWeightsRequest
    {
        public Dictionary<ClassificationType, FeatureWeightUpdateRequest> FeatureWeights { get; set; } = new();
    }

    public class FeatureWeightUpdateRequest
    {
        public float? UserWeight { get; set; }
        public Dictionary<Season, float>? SeasonalWeights { get; set; }
    }
}