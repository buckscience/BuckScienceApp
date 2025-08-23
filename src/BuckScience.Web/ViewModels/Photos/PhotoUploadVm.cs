namespace BuckScience.Web.ViewModels.Photos;

public class PhotoUploadVm
{
    public int PropertyId { get; set; }
    public int CameraId { get; set; }
    public IFormFile? File { get; set; }
    public string? Caption { get; set; }
}