using BuckScience.Application.Cameras;
using BuckScience.Application.Profiles;
using BuckScience.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BuckScience.Web.ViewModels.Properties;

public class PropertyDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    
    public IReadOnlyList<ListPropertyCameras.Result> Cameras { get; set; } = new List<ListPropertyCameras.Result>();
    public IReadOnlyList<ListPropertyProfiles.Result> Profiles { get; set; } = new List<ListPropertyProfiles.Result>();
    public IReadOnlyList<PropertyFeatureVm> Features { get; set; } = new List<PropertyFeatureVm>();
}

public class PropertyFeatureVm
{
    public int Id { get; set; }
    public ClassificationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string? GeometryWkt { get; set; }
    public string? Notes { get; set; }
    public DateTime? CreatedAt { get; set; }
}