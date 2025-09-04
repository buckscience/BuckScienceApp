namespace BuckScience.Web.ViewModels.Photos;

public class PhotoUploadVm
{
    public int PropertyId { get; set; }
    public int CameraId { get; set; }
    public IList<IFormFile>? Files { get; set; }
}