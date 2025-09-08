using BuckScience.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BuckScience.Web.ViewModels.PropertyFeatures;

public class PropertyFeatureEditVm
{
    [Required]
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    public string PropertyName { get; set; } = string.Empty;

    [StringLength(100)]
    [Display(Name = "Feature Name")]
    public string? Name { get; set; }

    [Required]
    [Display(Name = "Feature Type")]
    public ClassificationType ClassificationType { get; set; }

    [Required]
    public string GeometryWkt { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    // For display purposes
    public string GeometryType { get; set; } = string.Empty;

    public List<SelectListItem> ClassificationTypeOptions { get; set; } = new();
}