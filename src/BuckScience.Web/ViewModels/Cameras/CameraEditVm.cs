using System.ComponentModel.DataAnnotations;

namespace BuckScience.Web.ViewModels.Cameras;

public class CameraEditVm
{
    [Required, Range(1, int.MaxValue)]
    public int PropertyId { get; set; } // Provided by the route/context

    [Required]
    public int Id { get; set; }

    [Required, StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Model { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0, 360)]
    public float DirectionDegrees { get; set; }

    public bool IsActive { get; set; }
}