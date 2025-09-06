using BuckScience.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace BuckScience.Web.ViewModels.Properties;

public class PropertyFeatureEditVm
{
    [Required]
    public int Id { get; set; }

    [Required]
    public ClassificationType Type { get; set; }

    [StringLength(100)]
    public string? Name { get; set; }

    [Required]
    public string GeometryWkt { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Notes { get; set; }
}