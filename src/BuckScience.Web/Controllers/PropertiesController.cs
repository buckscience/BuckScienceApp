using BuckScience.Application.Abstractions;
using BuckScience.Application.Properties;
using BuckScience.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
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

        // Redirect somewhere useful; for now, back to Create with a flash message
        TempData["CreatedId"] = id;
        return RedirectToAction(nameof(Create));
    }
}