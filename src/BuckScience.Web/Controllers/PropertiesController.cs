using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Cameras;
using BuckScience.Application.Profiles;
using BuckScience.Application.Properties;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels;
using BuckScience.Web.ViewModels.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        return View(new PropertyCreateVm());
    }

    [HttpPost]
    [Route("/properties/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyCreateVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);
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

        return View(vm);
    }

    [HttpPost]
    [Route("/properties/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PropertyEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);
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
    [Route("/properties/{id:int}/details")]
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

        // Create feature placeholders based on ClassificationType enum
        var features = Enum.GetValues<ClassificationType>()
            .Select(type => new PropertyFeatureVm
            {
                Type = type,
                Name = GetFeatureName(type),
                Description = GetFeatureDescription(type),
                Icon = GetFeatureIcon(type)
            })
            .ToList();

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
}