using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Analytics;
using BuckScience.Application.Profiles;
using BuckScience.Application.Tags;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels.Profiles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Web.Controllers;

[Authorize]
public class ProfilesController : Controller
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly BuckLensAnalyticsService _analyticsService;

    public ProfilesController(IAppDbContext db, ICurrentUserService currentUser, BuckLensAnalyticsService analyticsService)
    {
        _db = db;
        _currentUser = currentUser;
        _analyticsService = analyticsService;
    }

    // DETAILS: GET /Profiles/{id}
    [HttpGet]
    [Route("/profiles/{id}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var profile = await GetProfile.HandleAsync(id, _db, _currentUser.Id.Value, ct);
        if (profile == null) return NotFound();

        var vm = new ProfileDetailsVm
        {
            Id = profile.Id,
            Name = profile.Name,
            ProfileStatus = profile.ProfileStatus,
            PropertyId = profile.PropertyId,
            PropertyName = profile.PropertyName,
            TagId = profile.TagId,
            TagName = profile.TagName,
            CoverPhotoUrl = profile.CoverPhotoUrl,
            TaggedPhotos = await _db.Photos
                .Where(p => _db.PhotoTags.Any(pt => pt.PhotoId == p.Id && pt.TagId == profile.TagId))
                .Join(_db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { p, c })
                .Where(pc => pc.c.PropertyId == profile.PropertyId)
                .Select(pc => new { 
                    Photo = pc.p, 
                    Camera = pc.c,
                    CurrentPlacement = pc.c.PlacementHistories.Where(ph => ph.EndDateTime == null).FirstOrDefault()
                })
                .Select(pc => new BuckScience.Web.ViewModels.Photos.PropertyPhotoListItemVm
                {
                    Id = pc.Photo.Id,
                    PhotoUrl = pc.Photo.PhotoUrl,
                    DateTaken = pc.Photo.DateTaken,
                    DateUploaded = pc.Photo.DateUploaded,
                    CameraId = pc.Camera.Id,
                    CameraLocationName = pc.CurrentPlacement != null ? pc.CurrentPlacement.LocationName : "",
                    Tags = _db.PhotoTags.Where(pt => pt.PhotoId == pc.Photo.Id)
                        .Join(_db.Tags, pt => pt.TagId, t => t.Id, (pt, t) => new BuckScience.Web.ViewModels.Photos.TagInfo { Id = t.Id, Name = t.TagName })
                        .ToList()
                })
                .OrderByDescending(x => x.DateTaken)
                .ToListAsync(ct)
        };

        ViewBag.SidebarWide = true;
        return View(vm);
    }

    // AJAX: POST /profiles/{id}/make-cover-photo
    [HttpPost]
    [Route("/profiles/{id}/make-cover-photo")]
    public async Task<IActionResult> MakeCoverPhoto(int id, [FromBody] MakeCoverPhotoRequest req, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        // Validate profile ownership
        var profile = await GetProfile.HandleAsync(id, _db, _currentUser.Id.Value, ct);
        if (profile == null) return NotFound();

        // Validate photo exists and is tagged for this profile
        var isTaggedPhoto = await _db.PhotoTags
            .AnyAsync(pt => pt.PhotoId == req.PhotoId && pt.TagId == profile.TagId, ct);
        if (!isTaggedPhoto) return BadRequest("Photo is not tagged for this profile.");

        // Set cover photo
        var dbProfile = await _db.Profiles.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (dbProfile == null) return NotFound();
        dbProfile.SetCoverPhoto(await _db.Photos.Where(p => p.Id == req.PhotoId).Select(p => p.PhotoUrl).FirstOrDefaultAsync(ct));
        await _db.SaveChangesAsync(ct);

        return Json(new { coverPhotoUrl = dbProfile.CoverPhotoUrl });
    }

    public class MakeCoverPhotoRequest
    {
        public int PhotoId { get; set; }
    }

    // CREATE: GET /Properties/{propertyId}/profiles/create
    [HttpGet]
    [Route("/properties/{propertyId}/profiles/create")]
    public async Task<IActionResult> Create(int propertyId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        // Validate property ownership
        var property = await _db.Properties
            .Where(p => p.Id == propertyId && p.ApplicationUserId == _currentUser.Id.Value)
            .FirstOrDefaultAsync(ct);

        if (property == null) return NotFound();

        // Get available tags for this property
        var availableTags = await ManagePhotoTags.GetAvailableTagsForPropertyAsync(propertyId, _db, ct);

        var vm = new ProfileCreateVm
        {
            PropertyId = propertyId,
            PropertyName = property.Name,
            AvailableTags = availableTags.Select(t => new SelectListItem 
            { 
                Value = t.Id.ToString(), 
                Text = t.Name 
            }).ToList(),
            ProfileStatusOptions = Enum.GetValues<ProfileStatus>()
                .Select(s => new SelectListItem 
                { 
                    Value = ((int)s).ToString(), 
                    Text = s.ToString() 
                }).ToList()
        };

        return View(vm);
    }

    // CREATE: POST /Properties/{propertyId}/profiles/create
    [HttpPost]
    [Route("/properties/{propertyId}/profiles/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int propertyId, ProfileCreateVm vm, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        if (!ModelState.IsValid)
        {
            // Reload dropdown data
            await PopulateCreateViewModelDropdowns(vm, propertyId, ct);
            return View(vm);
        }

        try
        {
            var cmd = new CreateProfile.Command(vm.Name, propertyId, vm.TagId, vm.ProfileStatus);
            var profileId = await CreateProfile.HandleAsync(cmd, _db, _currentUser.Id.Value, ct);

            TempData["SuccessMessage"] = "Profile created successfully.";
            
            // Redirect back to property details
            return RedirectToRoute("PropertyDetails", new { id = propertyId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            await PopulateCreateViewModelDropdowns(vm, propertyId, ct);
            return View(vm);
        }
    }

    // EDIT: GET /Profiles/{id}/edit
    [HttpGet]
    [Route("/profiles/{id}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var profile = await GetProfile.HandleAsync(id, _db, _currentUser.Id.Value, ct);
        if (profile == null) return NotFound();

        var vm = new ProfileEditVm
        {
            Id = profile.Id,
            Name = profile.Name,
            ProfileStatus = profile.ProfileStatus,
            PropertyId = profile.PropertyId,
            PropertyName = profile.PropertyName,
            TagId = profile.TagId,
            TagName = profile.TagName,
            CoverPhotoUrl = profile.CoverPhotoUrl,
            ProfileStatusOptions = Enum.GetValues<ProfileStatus>()
                .Select(s => new SelectListItem 
                { 
                    Value = ((int)s).ToString(), 
                    Text = s.ToString(),
                    Selected = s == profile.ProfileStatus
                }).ToList()
        };

        return View(vm);
    }

    // EDIT: POST /Profiles/{id}/edit
    [HttpPost]
    [Route("/profiles/{id}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProfileEditVm vm, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        if (!ModelState.IsValid)
        {
            // Reload dropdown data
            vm.ProfileStatusOptions = Enum.GetValues<ProfileStatus>()
                .Select(s => new SelectListItem 
                { 
                    Value = ((int)s).ToString(), 
                    Text = s.ToString(),
                    Selected = s == vm.ProfileStatus
                }).ToList();
            return View(vm);
        }

        try
        {
            var cmd = new UpdateProfile.Command(id, vm.Name, vm.ProfileStatus, vm.CoverPhotoUrl);
            await UpdateProfile.HandleAsync(cmd, _db, _currentUser.Id.Value, ct);

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", ex.Message);
            vm.ProfileStatusOptions = Enum.GetValues<ProfileStatus>()
                .Select(s => new SelectListItem 
                { 
                    Value = ((int)s).ToString(), 
                    Text = s.ToString(),
                    Selected = s == vm.ProfileStatus
                }).ToList();
            return View(vm);
        }
    }

    // DELETE: GET /Profiles/{id}/delete
    [HttpGet]
    [Route("/profiles/{id}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var profile = await GetProfile.HandleAsync(id, _db, _currentUser.Id.Value, ct);
        if (profile == null) return NotFound();

        var vm = new ProfileDeleteVm
        {
            Id = profile.Id,
            Name = profile.Name,
            ProfileStatus = profile.ProfileStatus,
            PropertyId = profile.PropertyId,
            PropertyName = profile.PropertyName,
            TagName = profile.TagName,
            CoverPhotoUrl = profile.CoverPhotoUrl
        };

        return View(vm);
    }

    // DELETE: POST /Profiles/{id}/delete
    [HttpPost, ActionName("Delete")]
    [Route("/profiles/{id}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            // Get the profile to know which property to redirect to
            var profile = await GetProfile.HandleAsync(id, _db, _currentUser.Id.Value, ct);
            if (profile == null) return NotFound();

            var cmd = new DeleteProfile.Command(id);
            await DeleteProfile.HandleAsync(cmd, _db, _currentUser.Id.Value, ct);

            TempData["SuccessMessage"] = "Profile deleted successfully.";
            return RedirectToRoute("PropertyDetails", new { id = profile.PropertyId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Delete), new { id });
        }
    }

    private async Task PopulateCreateViewModelDropdowns(ProfileCreateVm vm, int propertyId, CancellationToken ct)
    {
        // Get property name
        var property = await _db.Properties
            .Where(p => p.Id == propertyId && p.ApplicationUserId == _currentUser.Id!.Value)
            .FirstOrDefaultAsync(ct);

        if (property != null)
        {
            vm.PropertyName = property.Name;
        }

        // Get available tags
        var availableTags = await ManagePhotoTags.GetAvailableTagsForPropertyAsync(propertyId, _db, ct);
        vm.AvailableTags = availableTags.Select(t => new SelectListItem 
        { 
            Value = t.Id.ToString(), 
            Text = t.Name 
        }).ToList();

        // Set profile status options
        vm.ProfileStatusOptions = Enum.GetValues<ProfileStatus>()
            .Select(s => new SelectListItem 
            { 
                Value = ((int)s).ToString(), 
                Text = s.ToString() 
            }).ToList();
    }

    // BuckLens Analytics API Endpoints

    // API: GET /profiles/{id}/analytics/summary
    [HttpGet]
    [Route("/profiles/{id}/analytics/summary")]
    public async Task<IActionResult> GetAnalyticsSummary(int id, CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var data = await _analyticsService.GetProfileAnalyticsAsync(id, _currentUser.Id.Value, ct);
            var bestOdds = _analyticsService.GetBestOddsAnalysis(data);
            
            return Json(new
            {
                profileId = data.ProfileId,
                propertyName = data.PropertyName,
                totalSightings = data.TotalSightings,
                totalTaggedPhotos = data.TotalTaggedPhotos,
                dateRange = new
                {
                    start = data.DateRange.Start.ToString("yyyy-MM-dd"),
                    end = data.DateRange.End.ToString("yyyy-MM-dd")
                },
                bestOdds = new
                {
                    summary = bestOdds.Summary,
                    bestTimeOfDay = bestOdds.BestTimeOfDay,
                    bestCamera = bestOdds.BestCamera,
                    bestMoonPhase = bestOdds.BestMoonPhase,
                    bestWindDirection = bestOdds.BestWindDirection,
                    bestTemperatureRange = bestOdds.BestTemperatureRange
                }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // API: GET /profiles/{id}/analytics/charts/cameras
    [HttpGet]
    [Route("/profiles/{id}/analytics/charts/cameras")]
    public async Task<IActionResult> GetCameraChart(int id, CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var data = await _analyticsService.GetProfileAnalyticsAsync(id, _currentUser.Id.Value, ct);
            var chartData = _analyticsService.GetSightingsByCameraChart(data);
            return Json(chartData);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // API: GET /profiles/{id}/analytics/charts/timeofday
    [HttpGet]
    [Route("/profiles/{id}/analytics/charts/timeofday")]
    public async Task<IActionResult> GetTimeOfDayChart(int id, CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var data = await _analyticsService.GetProfileAnalyticsAsync(id, _currentUser.Id.Value, ct);
            var chartData = _analyticsService.GetSightingsByTimeOfDayChart(data);
            return Json(chartData);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // API: GET /profiles/{id}/analytics/charts/moonphase
    [HttpGet]
    [Route("/profiles/{id}/analytics/charts/moonphase")]
    public async Task<IActionResult> GetMoonPhaseChart(int id, CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var data = await _analyticsService.GetProfileAnalyticsAsync(id, _currentUser.Id.Value, ct);
            var chartData = _analyticsService.GetSightingsByMoonPhaseChart(data);
            return Json(chartData);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // API: GET /profiles/{id}/analytics/charts/winddirection
    [HttpGet]
    [Route("/profiles/{id}/analytics/charts/winddirection")]
    public async Task<IActionResult> GetWindDirectionChart(int id, CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var data = await _analyticsService.GetProfileAnalyticsAsync(id, _currentUser.Id.Value, ct);
            var chartData = _analyticsService.GetSightingsByWindDirectionChart(data);
            return Json(chartData);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // API: GET /profiles/{id}/analytics/charts/temperature
    [HttpGet]
    [Route("/profiles/{id}/analytics/charts/temperature")]
    public async Task<IActionResult> GetTemperatureChart(int id, CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var data = await _analyticsService.GetProfileAnalyticsAsync(id, _currentUser.Id.Value, ct);
            var chartData = _analyticsService.GetSightingsByTemperatureChart(data);
            return Json(chartData);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    // API: GET /profiles/{id}/analytics/sightings/locations
    [HttpGet]
    [Route("/profiles/{id}/analytics/sightings/locations")]
    public async Task<IActionResult> GetSightingLocations(int id, CancellationToken ct = default)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var data = await _analyticsService.GetProfileAnalyticsAsync(id, _currentUser.Id.Value, ct);
            var locations = data.Sightings
                .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
                .Select(s => new
                {
                    photoId = s.PhotoId,
                    dateTaken = s.DateTaken.ToString("yyyy-MM-dd HH:mm"),
                    cameraName = s.CameraName,
                    latitude = s.Latitude!.Value,
                    longitude = s.Longitude!.Value,
                    temperature = s.Temperature,
                    windDirection = s.WindDirectionText,
                    moonPhase = s.MoonPhaseText
                })
                .ToList();

            return Json(locations);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}