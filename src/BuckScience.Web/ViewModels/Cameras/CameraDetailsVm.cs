using BuckScience.Web.ViewModels.Photos;

namespace BuckScience.Web.ViewModels.Cameras;

public class CameraDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string? Model { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public float DirectionDegrees { get; set; }
    public DateTime? CurrentPlacementStartDate { get; set; }
    public TimeSpan? TimeAtCurrentLocation { get; set; }
    public bool IsActive { get; set; }
    public int PhotoCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
}