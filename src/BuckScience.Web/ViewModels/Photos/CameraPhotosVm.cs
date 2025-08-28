namespace BuckScience.Web.ViewModels.Photos;

public class PhotoListItemVm
{
    public int Id { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public DateTime DateTaken { get; set; }
    public DateTime DateUploaded { get; set; }
    
    /// <summary>
    /// Gets the properly encoded photo URL for display, handling spaces and special characters
    /// </summary>
    public string EncodedPhotoUrl
    {
        get
        {
            if (string.IsNullOrEmpty(PhotoUrl))
                return string.Empty;
            
            try
            {
                // If the URL already contains encoded characters, don't double-encode
                if (PhotoUrl.Contains("%20") || PhotoUrl.Contains("%"))
                    return PhotoUrl;
                
                // Parse the URL to separate the base URL from the filename
                var uri = new Uri(PhotoUrl);
                var lastSlashIndex = PhotoUrl.LastIndexOf('/');
                
                if (lastSlashIndex >= 0 && lastSlashIndex < PhotoUrl.Length - 1)
                {
                    var baseUrl = PhotoUrl.Substring(0, lastSlashIndex + 1);
                    var fileName = PhotoUrl.Substring(lastSlashIndex + 1);
                    var encodedFileName = Uri.EscapeDataString(fileName);
                    
                    return baseUrl + encodedFileName;
                }
                
                return PhotoUrl;
            }
            catch
            {
                // If URL parsing fails, return original URL
                return PhotoUrl;
            }
        }
    }
}

public class CameraPhotosVm
{
    public int CameraId { get; set; }
    public int PropertyId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public List<PhotoListItemVm> Photos { get; set; } = new();
    public List<CameraPhotoMonthGroup> PhotoGroups { get; set; } = new();
    public string CurrentSort { get; set; } = "DateTakenDesc";
    public int TotalPhotoCount { get; set; }
    
    // Tagging support
    public List<TagInfo> AvailableTags { get; set; } = new();
}

public class CameraPhotoMonthGroup
{
    public string MonthYear { get; set; } = string.Empty; // e.g., "October 2024"
    public List<PhotoListItemVm> Photos { get; set; } = new();
}

public static class CameraPhotoGroupingExtensions
{
    public static List<CameraPhotoMonthGroup> GroupByMonth(this List<Application.Photos.ListCameraPhotos.PhotoListItem> photos, bool isAscending = false)
    {
        var groups = photos.GroupBy(p => new { p.DateTaken.Year, p.DateTaken.Month });
        
        // Order groups based on sort direction
        var orderedGroups = isAscending 
            ? groups.OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            : groups.OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month);
        
        return orderedGroups
            .Select(g => new CameraPhotoMonthGroup
            {
                MonthYear = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                Photos = g.Select(p => new PhotoListItemVm
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    DateTaken = p.DateTaken,
                    DateUploaded = p.DateUploaded
                }).ToList()
            })
            .ToList();
    }
}