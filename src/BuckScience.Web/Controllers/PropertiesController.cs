using BuckScience.Application.Abstractions;
using BuckScience.Application.Properties;
using BuckScience.Web.ViewModels;
using BuckScience.Web.ViewModels.Properties;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Web.Controllers;

public class PropertiesController : Controller
{
    private readonly IAppDbContext _db;
    private readonly GeometryFactory _geometryFactory;

    public PropertiesController(IAppDbContext db, GeometryFactory geometryFactory)
    {
        _db = db;
        _geometryFactory = geometryFactory;
    }

    // LIST: GET /Properties
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var items = await ListProperties.HandleAsync(_db, ct);
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
    public IActionResult Create() => View(new PropertyCreateVm());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PropertyCreateVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var id = await CreateProperty.HandleAsync(
            new CreateProperty.Command(vm.Name, vm.Latitude, vm.Longitude, vm.TimeZone, vm.DayHour, vm.NightHour),
            _db,
            _geometryFactory,
            ct);

        TempData["CreatedId"] = id;
        return RedirectToAction(nameof(Index));
    }

    // EDIT
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var prop = await _db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
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
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PropertyEditVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        await UpdateProperty.HandleAsync(
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
            ct);

        TempData["UpdatedId"] = vm.Id;
        return RedirectToAction(nameof(Index));
    }

    // DELETE (confirm)
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var prop = await _db.Properties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
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
        await DeleteProperty.HandleAsync(id, _db, ct);
        TempData["DeletedId"] = id;
        return RedirectToAction(nameof(Index));
    }
}