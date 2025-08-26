using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Cameras;
using BuckScience.Application.Photos;
using BuckScience.Web.ViewModels.Cameras;
using BuckScience.Web.ViewModels.Photos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Web.Controllers;

[Authorize]
public class CamerasController : Controller
{
    private readonly IAppDbContext _db;
    private readonly GeometryFactory _geometryFactory;
    private readonly ICurrentUserService _currentUser;
    private readonly IBlobStorageService _blobStorageService;

    public CamerasController(IAppDbContext db, GeometryFactory geometryFactory, ICurrentUserService currentUser, IBlobStorageService blobStorageService)
    {
        _db = db;
        _geometryFactory = geometryFactory;
        _currentUser = currentUser;
        _blobStorageService = blobStorageService;
    }

    // LIST: GET /properties/{propertyId}/cameras
    [HttpGet]
    [Route("/properties/{propertyId:int}/cameras")]
    public async Task<IActionResult> Index(int propertyId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        // Verify property ownership and get name for header
        var prop = await _db.Properties.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.ApplicationUserId == _currentUser.Id.Value, ct);
        if (prop is null) return NotFound();

        // Uses Application handler that is property-scoped
        var items = await ListPropertyCameras.HandleAsync(_db, _currentUser.Id.Value, propertyId, ct);

        ViewBag.PropertyId = propertyId;
        ViewBag.PropertyName = prop.Name;

        var vms = items.Select(x => new CameraListItemVm
        {
            Id = x.Id,
            Name = x.Name,
            Brand = x.Brand,
            Model = x.Model,
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            IsActive = x.IsActive,
            PhotoCount = x.PhotoCount,
            CreatedDate = x.CreatedDate
        }).ToList();

        return View(vms);
    }

    // CREATE: GET /properties/{propertyId}/cameras/add
    [HttpGet]
    [Route("/properties/{propertyId:int}/cameras/add")]
    public async Task<IActionResult> Create(int propertyId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var owned = await _db.Properties.AsNoTracking()
            .AnyAsync(p => p.Id == propertyId && p.ApplicationUserId == _currentUser.Id.Value, ct);
        if (!owned) return NotFound();

        ViewBag.PropertyId = propertyId;
        return View(new CameraCreateVm { PropertyId = propertyId });
    }

    // CREATE: POST /properties/{propertyId}/cameras/add
    [HttpPost]
    [Route("/properties/{propertyId:int}/cameras/add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int propertyId, CameraCreateVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PropertyId = propertyId;
            return View(vm);
        }
        if (_currentUser.Id is null) return Forbid();

        // Route is the source of truth; ensure no mismatch with payload (defense-in-depth)
        if (vm.PropertyId != propertyId)
            return BadRequest("Route and model PropertyId mismatch.");

        var id = await CreateCamera.HandleAsync(
            new CreateCamera.Command(
                vm.Name,
                vm.Brand,
                vm.Model,
                vm.Latitude,
                vm.Longitude,
                vm.IsActive),
            _db,
            _geometryFactory,
            _currentUser.Id.Value,
            propertyId,
            ct);

        TempData["CreatedId"] = id;
        return RedirectToAction(nameof(Index), new { propertyId });
    }

    // EDIT: GET /properties/{propertyId}/cameras/{id}/edit
    [HttpGet]
    [Route("/properties/{propertyId:int}/cameras/{id:int}/edit")]
    public async Task<IActionResult> Edit(int propertyId, int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var cam = await _db.Cameras.AsNoTracking()
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.PropertyId == propertyId &&
                c.Property.ApplicationUserId == _currentUser.Id.Value, ct);

        if (cam is null) return NotFound();

        ViewBag.PropertyId = propertyId;
        ViewBag.PropertyName = cam.Property.Name;

        var vm = new CameraEditVm
        {
            PropertyId = propertyId,
            Id = cam.Id,
            Name = cam.Name,
            Brand = cam.Brand,
            Model = cam.Model,
            Latitude = cam.Latitude,
            Longitude = cam.Longitude,
            IsActive = cam.IsActive
        };

        return View(vm);
    }

    // EDIT: POST /properties/{propertyId}/cameras/{id}/edit
    [HttpPost]
    [Route("/properties/{propertyId:int}/cameras/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int propertyId, int id, CameraEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.PropertyId = propertyId;
            return View(vm);
        }
        if (_currentUser.Id is null) return Forbid();

        if (vm.PropertyId != propertyId || vm.Id != id)
            return BadRequest("Route and model identifiers mismatch.");

        // Optional pre-check for faster UX
        var owned = await _db.Cameras.AsNoTracking()
            .Include(c => c.Property)
            .AnyAsync(c =>
                c.Id == id &&
                c.PropertyId == propertyId &&
                c.Property.ApplicationUserId == _currentUser.Id.Value, ct);
        if (!owned) return NotFound();

        var ok = await UpdateCamera.HandleAsync(
            new UpdateCamera.Command(
                id,
                vm.Name,
                vm.Brand,
                vm.Model,
                vm.Latitude,
                vm.Longitude,
                vm.IsActive),
            _db,
            _geometryFactory,
            _currentUser.Id.Value,
            propertyId,
            ct);

        if (!ok) return NotFound();

        TempData["UpdatedId"] = id;
        return RedirectToAction(nameof(Index), new { propertyId });
    }

    // DELETE (confirm): GET /properties/{propertyId}/cameras/{id}/delete
    [HttpGet]
    [Route("/properties/{propertyId:int}/cameras/{id:int}/delete")]
    public async Task<IActionResult> Delete(int propertyId, int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var cam = await _db.Cameras.AsNoTracking()
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.PropertyId == propertyId &&
                c.Property.ApplicationUserId == _currentUser.Id.Value, ct);

        if (cam is null) return NotFound();

        var vm = new CameraDeleteVm
        {
            PropertyId = propertyId,
            Id = cam.Id,
            Name = cam.Name,
            Brand = cam.Brand,
            Model = cam.Model,
            Latitude = cam.Latitude,
            Longitude = cam.Longitude,
            IsActive = cam.IsActive,
            PropertyName = cam.Property.Name
        };

        ViewBag.PropertyId = propertyId;
        return View(vm);
    }

    // DELETE (execute): POST /properties/{propertyId}/cameras/{id}/delete
    [HttpPost, ActionName("Delete")]
    [Route("/properties/{propertyId:int}/cameras/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int propertyId, int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var ok = await DeleteCamera.HandleAsync(propertyId, id, _currentUser.Id.Value, _db, ct);
        if (!ok) return NotFound();

        TempData["DeletedId"] = id;
        return RedirectToAction(nameof(Index), new { propertyId });
    }

    // UPLOAD PHOTO: GET
    [HttpGet("/cameras/{id:int}/upload")]
    public async Task<IActionResult> UploadPhoto([FromRoute] int id, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        // NOTE: Adjust the owner field name to your schema:
        // If your Property entity uses ApplicationUserId (as your snippet shows), keep it.
        // If it uses OwnerUserId (common in your other controllers), swap accordingly.
        var cam = await _db.Cameras
            .AsNoTracking()
            .Include(c => c.Property)
            .FirstOrDefaultAsync(
                c => c.Id == id
                     && c.Property != null
                     && c.Property.ApplicationUserId == _currentUser.Id.Value, // or: c.Property.OwnerUserId == _currentUser.Id.Value
                ct);

        if (cam is null) return NotFound();

        ViewBag.PropertyId = cam.PropertyId;
        ViewBag.CameraId = cam.Id;
        ViewBag.CameraName = cam.Name;
        ViewBag.PropertyName = cam.Property?.Name;

        var vm = new PhotoUploadVm
        {
            PropertyId = cam.PropertyId,
            CameraId = cam.Id
        };

        return View("Upload", vm);
    }

    // UPLOAD PHOTO: POST
    [HttpPost("/cameras/{id:int}/photos/upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadPhoto([FromRoute] int id, PhotoUploadVm vm, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        // Validate route matches model
        if (id != vm.CameraId)
            return BadRequest("Route and model CameraId mismatch.");

        if (!ModelState.IsValid)
        {
            // Reload view data for error display
            var cam = await _db.Cameras
                .AsNoTracking()
                .Include(c => c.Property)
                .FirstOrDefaultAsync(
                    c => c.Id == id && c.Property != null && c.Property.ApplicationUserId == _currentUser.Id.Value, ct);

            if (cam is null) return NotFound();

            ViewBag.PropertyId = cam.PropertyId;
            ViewBag.CameraId = cam.Id;
            ViewBag.CameraName = cam.Name;
            ViewBag.PropertyName = cam.Property?.Name;

            return View("Upload", vm);
        }

        if (vm.Files == null || !vm.Files.Any())
        {
            ModelState.AddModelError("Files", "Please select at least one file to upload.");
            
            // Reload view data for error display
            var cam = await _db.Cameras
                .AsNoTracking()
                .Include(c => c.Property)
                .FirstOrDefaultAsync(
                    c => c.Id == id && c.Property != null && c.Property.ApplicationUserId == _currentUser.Id.Value, ct);

            if (cam is null) return NotFound();

            ViewBag.PropertyId = cam.PropertyId;
            ViewBag.CameraId = cam.Id;
            ViewBag.CameraName = cam.Name;
            ViewBag.PropertyName = cam.Property?.Name;

            return View("Upload", vm);
        }

        try
        {
            // Convert IFormFile to FileData for the application layer
            var fileDataList = new List<UploadPhotos.FileData>();
            foreach (var file in vm.Files)
            {
                if (file.Length > 0)
                {
                    fileDataList.Add(new UploadPhotos.FileData(
                        file.FileName,
                        file.OpenReadStream(),
                        file.Length
                    ));
                }
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            
            var photoIds = await UploadPhotos.HandleAsync(
                new UploadPhotos.Command(vm.CameraId, fileDataList, vm.Caption),
                _db,
                _currentUser.Id.Value,
                _blobStorageService,
                ct);

            TempData["UploadedPhotos"] = photoIds.Count;
            
            // Check if this is from setup flow
            if (HttpContext.Request.Query["fromSetup"] == "1")
            {
                // Redirect to cameras list for the property
                return RedirectToAction("Index", "Cameras", new { propertyId = vm.PropertyId, fromSetup = 1 });
            }

            return RedirectToAction("Index", "Cameras", new { propertyId = vm.PropertyId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Upload failed: {ex.Message}");
            
            // Reload view data for error display
            var cam = await _db.Cameras
                .AsNoTracking()
                .Include(c => c.Property)
                .FirstOrDefaultAsync(
                    c => c.Id == id && c.Property != null && c.Property.ApplicationUserId == _currentUser.Id.Value, ct);

            if (cam is null) return NotFound();

            ViewBag.PropertyId = cam.PropertyId;
            ViewBag.CameraId = cam.Id;
            ViewBag.CameraName = cam.Name;
            ViewBag.PropertyName = cam.Property?.Name;

            return View("Upload", vm);
        }
    }
}