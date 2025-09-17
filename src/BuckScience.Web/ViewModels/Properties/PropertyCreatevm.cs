using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BuckScience.Web.ViewModels.Properties;

public sealed class PropertyCreateVm
{
    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Required, StringLength(100)]
    public string TimeZone { get; set; } = "UTC";

    [Range(0, 23)]
    public int DayHour { get; set; } = 8;

    [Range(0, 23)]
    public int NightHour { get; set; } = 20;

    // For timezone dropdown
    public List<SelectListItem> TimeZones { get; set; } = new();
}