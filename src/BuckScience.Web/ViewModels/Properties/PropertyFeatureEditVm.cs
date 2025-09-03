using BuckScience.Domain.Enums;

namespace BuckScience.Web.ViewModels.Properties;

public class PropertyFeatureEditVm
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public ClassificationType ClassificationType { get; set; }
    public string GeometryWkt { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string GeometryType { get; set; } = string.Empty;
}