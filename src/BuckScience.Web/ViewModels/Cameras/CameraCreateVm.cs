using System.ComponentModel.DataAnnotations;
using BuckScience.Web.Helpers;

namespace BuckScience.Web.ViewModels.Cameras;

public class CameraCreateVm
{
    [Required, Range(1, int.MaxValue)]
    public int PropertyId { get; set; } // Provided by the route/context

    [Required, StringLength(200)]
    [Display(Name = "Location Name")]
    public string LocationName { get; set; } = string.Empty;

    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Model { get; set; }

    [Range(-90, 90)]
    public double Latitude { get; set; }

    [Range(-180, 180)]
    public double Longitude { get; set; }

    [Range(0, 360)]
    public float DirectionDegrees { get; set; } = 0f;

    // New property for direction selection via compass directions
    [Required]
    [Display(Name = "Direction")]
    public DirectionHelper.CompassDirection DirectionSelection { get; set; } = DirectionHelper.CompassDirection.N;

    public bool IsActive { get; set; } = true;

    // Property coordinates for map centering (not editable by user)
    public double PropertyLatitude { get; set; }
    public double PropertyLongitude { get; set; }

    // Helper method to sync DirectionDegrees from DirectionSelection
    public void SyncDirectionFromSelection()
    {
        DirectionDegrees = DirectionHelper.ToFloat(DirectionSelection);
    }

    // Helper method to sync DirectionSelection from DirectionDegrees
    public void SyncSelectionFromDirection()
    {
        DirectionSelection = DirectionHelper.FromFloat(DirectionDegrees);
    }
}