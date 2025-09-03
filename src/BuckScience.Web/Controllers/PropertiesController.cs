using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Cameras;
using BuckScience.Application.Photos;
using BuckScience.Application.Profiles;
using BuckScience.Application.Properties;
using BuckScience.Application.PropertyFeatures;
using BuckScience.Application.Tags;
using BuckScience.Domain.Enums;
using BuckScience.Web.Helpers;
using BuckScience.Web.ViewModels;
using BuckScience.Web.ViewModels.Photos;
using BuckScience.Web.ViewModels.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Web.Controllers;

[Authorize]
public class PropertiesController : Controller
{
    private readonly IAppDbContext _db;
    private readonly GeometryFactory _geometryFactory;
    private readonly ICurrentUserService _currentUser;

    public PropertiesController(IAppDbContext db, GeometryFactory geometryFactory, ICurrentUserService currentUser)
    {
        _db = db;
        _geometryFactory = geometryFactory;
        _currentUser = currentUser;
    }

    // LIST: GET /Properties
    [HttpGet]
    [Route("/properties")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var items = await ListProperties.HandleAsync(_db, _currentUser.Id.Value, ct);

        var vms = items.Select(x => new PropertyListItemVm
        {
            Id = x.Id,
            Name = x.Name,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            TimeZone = x.TimeZone,
            DayHour = x.DayHour,
            NightHour = x.NightHour
        }).ToList();

        return View(vms);
    }

    // CREATE
    [HttpGet]
    [Route("/properties/add")]
    public IActionResult Create()
    {
        if (_currentUser.Id is null) return Forbid();
        
        var vm = new PropertyCreateVm();
        PopulateTimeZones(vm);
        return View(vm);
    }

    [HttpPost]
    [Route("/properties/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyCreateVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) 
        {
            PopulateTimeZones(vm);
            return View(vm);
        }
        if (_currentUser.Id is null) return Forbid();

        var id = await CreateProperty.HandleAsync(
            new CreateProperty.Command(vm.Name, vm.Latitude, vm.Longitude, vm.TimeZone, vm.DayHour, vm.NightHour),
            _db,
            _geometryFactory,
            _currentUser.Id.Value,
            ct);

        TempData["CreatedId"] = id;
        return RedirectToAction(nameof(Index));
    }

    // EDIT
    [HttpGet]
    [Route("/properties/{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var prop = await _db.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.ApplicationUserId == _currentUser.Id.Value, ct);
        if (prop is null) return NotFound();

        var vm = new PropertyEditVm
        {
            Id = prop.Id,
            Name = prop.Name,
            Latitude = prop.Latitude,
            Longitude = prop.Longitude,
            TimeZone = prop.TimeZone,
            DayHour = prop.DayHour,
            NightHour = prop.NightHour
        };

        PopulateTimeZones(vm);
        return View(vm);
    }

    [HttpPost]
    [Route("/properties/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PropertyEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) 
        {
            PopulateTimeZones(vm);
            return View(vm);
        }
        if (_currentUser.Id is null) return Forbid();

        // Optional pre-check for faster UX
        var owned = await _db.Properties.AsNoTracking()
            .AnyAsync(p => p.Id == vm.Id && p.ApplicationUserId == _currentUser.Id.Value, ct);
        if (!owned) return NotFound();

        var ok = await UpdateProperty.HandleAsync(
            new UpdateProperty.Command(
                vm.Id,
                vm.Name,
                vm.Latitude,
                vm.Longitude,
                vm.TimeZone,
                vm.DayHour,
                vm.NightHour),
            _db,
            _geometryFactory,
            _currentUser.Id.Value,
            ct);

        if (!ok) return NotFound();

        TempData["UpdatedId"] = vm.Id;
        return RedirectToAction(nameof(Index));
    }

    // DETAILS
    [HttpGet]
    [Route("/properties/{id:int}/details", Name = "PropertyDetails")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var prop = await _db.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.ApplicationUserId == _currentUser.Id.Value, ct);
        if (prop is null) return NotFound();

        // Get cameras for this property
        var cameras = await ListPropertyCameras.HandleAsync(_db, _currentUser.Id.Value, id, ct);
        
        // Get profiles for this property
        var profiles = await ListPropertyProfiles.HandleAsync(_db, _currentUser.Id.Value, id, ct);

        // Get actual features for this property
        var propertyFeatures = await ListPropertyFeatures.HandleAsync(_db, _currentUser.Id.Value, id, ct);
        var features = propertyFeatures.Select(pf => new PropertyFeatureVm
        {
            Id = pf.Id,
            Type = pf.ClassificationType,
            Name = GetFeatureName(pf.ClassificationType),
            Description = GetFeatureDescription(pf.ClassificationType),
            Icon = GetFeatureIcon(pf.ClassificationType),
            GeometryWkt = pf.GeometryWkt,
            Notes = pf.Notes,
            CreatedAt = pf.CreatedAt
        }).ToList();

        var vm = new PropertyDetailsVm
        {
            Id = prop.Id,
            Name = prop.Name,
            Cameras = cameras,
            Profiles = profiles,
            Features = features
        };

        return View(vm);
    }

    private static string GetFeatureName(ClassificationType type) => type switch
    {
        ClassificationType.BeddingArea => "Bedding Area",
        ClassificationType.FeedingZone => "Feeding Zone",
        ClassificationType.TravelCorridor => "Travel Corridor",
        ClassificationType.PinchPointFunnel => "Pinch Point/Funnel",
        ClassificationType.WaterSource => "Water Source",
        ClassificationType.SecurityCover => "Security Cover",
        ClassificationType.Other => "Other",
        _ => type.ToString()
    };

    private static string GetFeatureDescription(ClassificationType type) => type switch
    {
        ClassificationType.BeddingArea => "Areas where deer rest during the day",
        ClassificationType.FeedingZone => "Primary feeding and foraging areas",
        ClassificationType.TravelCorridor => "Paths deer use to move between areas",
        ClassificationType.PinchPointFunnel => "Natural funnels that concentrate deer movement",
        ClassificationType.WaterSource => "Water sources like creeks, ponds, or springs",
        ClassificationType.SecurityCover => "Thick cover that provides security for deer",
        ClassificationType.Other => "Other important features on the property",
        _ => $"Features related to {type}"
    };

    private static string GetFeatureIcon(ClassificationType type) => type switch
    {
        ClassificationType.BeddingArea => "fas fa-bed",
        ClassificationType.FeedingZone => "fas fa-seedling",
        ClassificationType.TravelCorridor => "fas fa-route",
        ClassificationType.PinchPointFunnel => "fas fa-compress-arrows-alt",
        ClassificationType.WaterSource => "fas fa-tint",
        ClassificationType.SecurityCover => "fas fa-shield-alt",
        ClassificationType.Other => "fas fa-map-pin",
        _ => "fas fa-map-pin"
    };

    // PHOTOS: GET /properties/{id}/photos
    [HttpGet]
    [Route("/properties/{id:int}/photos")]
    public async Task<IActionResult> Photos(
        int id,
        [FromQuery] string sort = "DateTakenDesc",
        // Date filters
        [FromQuery] DateTime? dateTakenFrom = null,
        [FromQuery] DateTime? dateTakenTo = null,
        [FromQuery] DateTime? dateUploadedFrom = null,
        [FromQuery] DateTime? dateUploadedTo = null,
        // Camera filters
        [FromQuery] string? cameras = null, // comma-separated camera IDs
        // Weather filters
        [FromQuery] double? tempMin = null,
        [FromQuery] double? tempMax = null,
        [FromQuery] double? windSpeedMin = null,
        [FromQuery] double? windSpeedMax = null,
        [FromQuery] double? humidityMin = null,
        [FromQuery] double? humidityMax = null,
        [FromQuery] double? pressureMin = null,
        [FromQuery] double? pressureMax = null,
        [FromQuery] double? visibilityMin = null,
        [FromQuery] double? visibilityMax = null,
        [FromQuery] double? cloudCoverMin = null,
        [FromQuery] double? cloudCoverMax = null,
        [FromQuery] double? moonPhaseMin = null,
        [FromQuery] double? moonPhaseMax = null,
        [FromQuery] string? conditions = null, // comma-separated conditions
        [FromQuery] string? moonPhaseTexts = null, // comma-separated moon phase texts
        [FromQuery] string? pressureTrends = null, // comma-separated pressure trends
        [FromQuery] string? windDirections = null, // comma-separated wind directions
        CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        // Verify property ownership
        var property = await _db.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.ApplicationUserId == _currentUser.Id.Value, ct);
        if (property is null) return NotFound();

        // Get sort option
        var sortBy = sort switch
        {
            "DateTakenAsc" => ListPropertyPhotos.SortBy.DateTakenAsc,
            "DateTakenDesc" => ListPropertyPhotos.SortBy.DateTakenDesc,
            "DateUploadedAsc" => ListPropertyPhotos.SortBy.DateUploadedAsc,
            "DateUploadedDesc" => ListPropertyPhotos.SortBy.DateUploadedDesc,
            _ => ListPropertyPhotos.SortBy.DateTakenDesc
        };

        // Build filters from query parameters
        var filters = BuildFilters(
            dateTakenFrom, dateTakenTo, dateUploadedFrom, dateUploadedTo,
            cameras, tempMin, tempMax, windSpeedMin, windSpeedMax,
            humidityMin, humidityMax, pressureMin, pressureMax,
            visibilityMin, visibilityMax, cloudCoverMin, cloudCoverMax,
            moonPhaseMin, moonPhaseMax, conditions, moonPhaseTexts,
            pressureTrends, windDirections);

        // Get all photos from all cameras on this property
        var photos = await ListPropertyPhotos.HandleAsync(_db, _currentUser.Id.Value, id, sortBy, filters, ct);

        // Group photos by month/year with proper sort direction
        var isAscending = sort == "DateTakenAsc" || sort == "DateUploadedAsc";
        var photoGroups = photos.GroupByMonth(isAscending);

        // Get available filter options for this property
        var availableOptions = await GetAvailableFilterOptions(_db, _currentUser.Id.Value, id, ct);
        
        // Get available tags for this property
        var availableTags = await ManagePhotoTags.GetAvailableTagsForPropertyAsync(id, _db, ct);

        var vm = new PropertyPhotosVm
        {
            PropertyId = property.Id,
            PropertyName = property.Name,
            PhotoGroups = photoGroups,
            CurrentSort = sort,
            TotalPhotoCount = photos.Count(),
            AppliedFilters = filters,
            AvailableCameras = availableOptions.Cameras,
            AvailableConditions = availableOptions.Conditions,
            AvailableMoonPhases = availableOptions.MoonPhases,
            AvailablePressureTrends = availableOptions.PressureTrends,
            AvailableWindDirections = availableOptions.WindDirections,
            WindDirectionOptions = WeatherHelpers.GetWindDirectionOptions(availableOptions.WindDirections),
            AvailableTags = availableTags.Select(t => new TagInfo { Id = t.Id, Name = t.Name }).ToList()
        };

        return View(vm);
    }

    // DELETE (confirm)
    [HttpGet]
    [Route("/properties/{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var prop = await _db.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.ApplicationUserId == _currentUser.Id.Value, ct);
        if (prop is null) return NotFound();

        var vm = new PropertyDeleteVm
        {
            Id = prop.Id,
            Name = prop.Name,
            Latitude = prop.Latitude,
            Longitude = prop.Longitude,
            TimeZone = prop.TimeZone,
            DayHour = prop.DayHour,
            NightHour = prop.NightHour
        };

        return View(vm);
    }

    // DELETE (execute)
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var ok = await DeleteProperty.HandleAsync(id, _currentUser.Id.Value, _db, ct);
        if (!ok) return NotFound();

        TempData["DeletedId"] = id;
        return RedirectToAction(nameof(Index));
    }

    private static async Task<(List<CameraOption> Cameras, List<string> Conditions, List<string> MoonPhases, List<string> PressureTrends, List<string> WindDirections)> 
        GetAvailableFilterOptions(IAppDbContext db, int userId, int propertyId, CancellationToken ct)
    {
        // Get cameras for this property using explicit join
        var cameras = await db.Cameras
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .Where(x => x.Camera.PropertyId == propertyId && x.Property.ApplicationUserId == userId)
            .Select(x => new CameraOption { Id = x.Camera.Id, Name = x.Camera.Name })
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        // Get distinct weather conditions from photos with weather data using explicit joins
        var weatherData = await db.Photos
            .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { Photo = p, Camera = c })
            .Join(db.Properties, x => x.Camera.PropertyId, prop => prop.Id, (x, prop) => new { x.Photo, x.Camera, Property = prop })
            .Join(db.Weathers, x => x.Photo.WeatherId, w => w.Id, (x, w) => new { x.Photo, x.Camera, x.Property, Weather = w })
            .Where(x => x.Camera.PropertyId == propertyId && 
                       x.Property.ApplicationUserId == userId)
            .Select(x => x.Weather)
            .ToListAsync(ct);

        var conditions = weatherData
            .Select(w => w.Conditions)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var moonPhases = weatherData
            .Select(w => w.MoonPhaseText)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct()
            .OrderBy(m => m)
            .ToList();

        var pressureTrends = weatherData
            .Select(w => w.PressureTrend)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct()
            .OrderBy(p => p)
            .ToList();

        var windDirections = weatherData
            .Select(w => w.WindDirectionText)
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Distinct()
            .OrderBy(w => w)
            .ToList();

        return (cameras, conditions, moonPhases, pressureTrends, windDirections);
    }

    private static PhotoFilters? BuildFilters(
        DateTime? dateTakenFrom, DateTime? dateTakenTo,
        DateTime? dateUploadedFrom, DateTime? dateUploadedTo,
        string? cameras, double? tempMin, double? tempMax,
        double? windSpeedMin, double? windSpeedMax,
        double? humidityMin, double? humidityMax,
        double? pressureMin, double? pressureMax,
        double? visibilityMin, double? visibilityMax,
        double? cloudCoverMin, double? cloudCoverMax,
        double? moonPhaseMin, double? moonPhaseMax,
        string? conditions, string? moonPhaseTexts,
        string? pressureTrends, string? windDirections)
    {
        var filters = new PhotoFilters
        {
            DateTakenFrom = dateTakenFrom,
            DateTakenTo = dateTakenTo,
            DateUploadedFrom = dateUploadedFrom,
            DateUploadedTo = dateUploadedTo,
            TemperatureMin = tempMin,
            TemperatureMax = tempMax,
            WindSpeedMin = windSpeedMin,
            WindSpeedMax = windSpeedMax,
            HumidityMin = humidityMin,
            HumidityMax = humidityMax,
            PressureMin = pressureMin,
            PressureMax = pressureMax,
            VisibilityMin = visibilityMin,
            VisibilityMax = visibilityMax,
            CloudCoverMin = cloudCoverMin,
            CloudCoverMax = cloudCoverMax,
            MoonPhaseMin = moonPhaseMin,
            MoonPhaseMax = moonPhaseMax
        };

        // Parse comma-separated lists
        if (!string.IsNullOrWhiteSpace(cameras))
        {
            var cameraIds = cameras.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (cameraIds.Count > 0)
                filters.CameraIds = cameraIds;
        }

        if (!string.IsNullOrWhiteSpace(conditions))
        {
            filters.Conditions = conditions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(moonPhaseTexts))
        {
            filters.MoonPhaseTexts = moonPhaseTexts.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(pressureTrends))
        {
            filters.PressureTrends = pressureTrends.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(windDirections))
        {
            filters.WindDirectionTexts = windDirections.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        // Return null if no filters are applied to avoid unnecessary processing
        return filters.HasAnyFilters ? filters : null;
    }

    private void PopulateTimeZones(PropertyCreateVm vm)
    {
        var timeZones = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new SelectListItem
            {
                Value = tz.Id,
                Text = $"({tz.BaseUtcOffset:hh\\:mm}) {tz.DisplayName}",
                Selected = tz.Id == vm.TimeZone
            })
            .OrderBy(x => x.Text)
            .ToList();

        vm.TimeZones = timeZones;
    }

    private void PopulateTimeZones(PropertyEditVm vm)
    {
        var timeZones = TimeZoneInfo.GetSystemTimeZones()
            .Select(tz => new SelectListItem
            {
                Value = tz.Id,
                Text = $"({tz.BaseUtcOffset:hh\\:mm}) {tz.DisplayName}",
                Selected = tz.Id == vm.TimeZone
            })
            .OrderBy(x => x.Text)
            .ToList();

        vm.TimeZones = timeZones;
    }
}