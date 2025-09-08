using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.PropertyFeatures;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels.PropertyFeatures;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Web.Controllers;

[Authorize]
public class PropertyFeaturesController : Controller
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

    // EDIT: GET /features/{id}/edit
    [HttpGet]
    [Route("/features/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var feature = await Application.PropertyFeatures.GetPropertyFeature.HandleAsync(id, _db, _currentUser.Id.Value, ct);
        if (feature is null) return NotFound();

        // Get property name for breadcrumb/header
        var property = await _db.Properties.AsNoTracking()
            .Where(p => p.Id == feature.PropertyId)
            .FirstOrDefaultAsync(ct);

        var vm = new PropertyFeatureEditVm
        {
            Id = feature.Id,
            PropertyId = feature.PropertyId,
            PropertyName = property?.Name ?? "Unknown",
            Name = feature.Name,
            ClassificationType = feature.ClassificationType,
            GeometryWkt = feature.GeometryWkt,
            Notes = feature.Notes,
            GeometryType = ExtractGeometryType(feature.GeometryWkt),
            ClassificationTypeOptions = GetClassificationTypeOptions(feature.ClassificationType)
        };

        return View(vm);
    }

    // EDIT: POST /features/{id}/edit
    [HttpPost]
    [Route("/features/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PropertyFeatureEditVm vm, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        if (vm.Id != id)
            return BadRequest("Route and model Id mismatch.");

        if (!ModelState.IsValid)
        {
            vm.ClassificationTypeOptions = GetClassificationTypeOptions(vm.ClassificationType);
            return View(vm);
        }

        try
        {
            var cmd = new Application.PropertyFeatures.UpdatePropertyFeature.Command(
                id,
                vm.ClassificationType,
                vm.GeometryWkt,
                vm.Name,
                vm.Notes);

            var success = await Application.PropertyFeatures.UpdatePropertyFeature.HandleAsync(cmd, _db, _geometryFactory, _currentUser.Id.Value, ct);
            if (!success) return NotFound();

            TempData["SuccessMessage"] = "Feature updated successfully.";
            return RedirectToRoute("PropertyDetails", new { id = vm.PropertyId });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.ClassificationTypeOptions = GetClassificationTypeOptions(vm.ClassificationType);
            return View(vm);
        }
    }

    private static string ExtractGeometryType(string geometryWkt)
    {
        if (string.IsNullOrEmpty(geometryWkt)) return "Unknown";
        
        var upperWkt = geometryWkt.ToUpper();
        if (upperWkt.StartsWith("POINT")) return "Point";
        if (upperWkt.StartsWith("LINESTRING")) return "Line";
        if (upperWkt.StartsWith("POLYGON")) return "Polygon";
        if (upperWkt.StartsWith("MULTIPOINT")) return "MultiPoint";
        if (upperWkt.StartsWith("MULTILINESTRING")) return "MultiLine";
        if (upperWkt.StartsWith("MULTIPOLYGON")) return "MultiPolygon";
        
        return "Unknown";
    }

    private static List<SelectListItem> GetClassificationTypeOptions(ClassificationType selected)
    {
        return Enum.GetValues<ClassificationType>()
            .Select(ct => new SelectListItem
            {
                Value = ((int)ct).ToString(),
                Text = GetClassificationTypeDisplayName(ct),
                Selected = ct == selected
            })
            .OrderBy(x => x.Text)
            .ToList();
    }

    private static string GetClassificationTypeDisplayName(ClassificationType type)
    {
        return type switch
        {
            ClassificationType.AgCropField => "Agricultural Crop Field",
            ClassificationType.FoodPlot => "Food Plot",
            ClassificationType.MastTreePatch => "Mast Tree Patch",
            ClassificationType.BrowsePatch => "Browse Patch",
            ClassificationType.PrairieForbPatch => "Prairie Forb Patch",
            ClassificationType.BeddingArea => "Bedding Area",
            ClassificationType.ThickBrush => "Thick Brush",
            ClassificationType.Clearcut => "Clearcut",
            ClassificationType.CRP => "CRP (Conservation Reserve Program)",
            ClassificationType.CedarThicket => "Cedar Thicket",
            ClassificationType.LeewardSlope => "Leeward Slope",
            ClassificationType.EdgeCover => "Edge Cover",
            ClassificationType.IsolatedCover => "Isolated Cover",
            ClassificationType.ManMadeCover => "Man-Made Cover",
            ClassificationType.RidgePoint => "Ridge Point",
            ClassificationType.RidgeSpur => "Ridge Spur",
            ClassificationType.CreekCrossing => "Creek Crossing",
            ClassificationType.FieldEdge => "Field Edge",
            ClassificationType.InsideCorner => "Inside Corner",
            ClassificationType.PinchPointFunnel => "Pinch Point/Funnel",
            ClassificationType.TravelCorridor => "Travel Corridor",
            _ => type.ToString()
        };
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