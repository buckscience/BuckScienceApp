using BuckScience.Application.Cameras;
using BuckScience.Application.Profiles;
using BuckScience.Domain.Enums;

namespace BuckScience.Web.ViewModels.Properties;

public class PropertyDetailsVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public IReadOnlyList<ListPropertyCameras.Result> Cameras { get; set; } = new List<ListPropertyCameras.Result>();
    public IReadOnlyList<ListPropertyProfiles.Result> Profiles { get; set; } = new List<ListPropertyProfiles.Result>();
    public IReadOnlyList<PropertyFeatureVm> Features { get; set; } = new List<PropertyFeatureVm>();
}

public class PropertyFeatureVm
{
    public ClassificationType Type { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}