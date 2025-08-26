namespace BuckScience.Web.ViewModels.Photos;

public class PhotoListItemVm
{
    public int Id { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public DateTime DateTaken { get; set; }
    public DateTime DateUploaded { get; set; }
}

public class CameraPhotosVm
{
    public int CameraId { get; set; }
    public int PropertyId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public List<PhotoListItemVm> Photos { get; set; } = new();
}