using BuckScience.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BuckScience.Web.ViewModels.Profiles;

public class ProfileEditVm
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ProfileStatus ProfileStatus { get; set; }

    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? CoverPhotoUrl { get; set; }

    public List<SelectListItem> ProfileStatusOptions { get; set; } = new();
}