using BuckScience.Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BuckScience.Web.ViewModels.Profiles;

public class ProfileCreateVm
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int TagId { get; set; }

    [Required]
    public ProfileStatus ProfileStatus { get; set; } = ProfileStatus.Watching;

    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;

    public List<SelectListItem> AvailableTags { get; set; } = new();
    public List<SelectListItem> ProfileStatusOptions { get; set; } = new();
}