using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
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

    public ProfilesController(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
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
                .Select(pc => new BuckScience.Web.ViewModels.Photos.PropertyPhotoListItemVm
                {
                    Id = pc.p.Id,
                    PhotoUrl = pc.p.PhotoUrl,
                    DateTaken = pc.p.DateTaken,
                    DateUploaded = pc.p.DateUploaded,
                    CameraId = pc.c.Id,
                    CameraName = pc.c.Name,
                    Tags = _db.PhotoTags.Where(pt => pt.PhotoId == pc.p.Id)
                        .Join(_db.Tags, pt => pt.TagId, t => t.Id, (pt, t) => new BuckScience.Web.ViewModels.Photos.TagInfo { Id = t.Id, Name = t.TagName })
                        .ToList()
                })
                .OrderByDescending(x => x.DateTaken)
                .ToListAsync(ct)
        };

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
}