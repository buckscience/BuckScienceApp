using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Properties;
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