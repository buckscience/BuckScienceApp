using BuckScience.Domain.Enums;

namespace BuckScience.Web.ViewModels.Profiles;

public class ProfileDeleteVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProfileStatus ProfileStatus { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string TagName { get; set; } = string.Empty;
    public string? CoverPhotoUrl { get; set; }
}