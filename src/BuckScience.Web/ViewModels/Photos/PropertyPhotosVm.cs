using BuckScience.Application.Photos;
using BuckScience.Web.Helpers;
using WindDirectionOption = BuckScience.Shared.Helpers.WindDirectionOption;

namespace BuckScience.Web.ViewModels.Photos;

public class PropertyPhotosVm
{
    public int PropertyId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public List<PhotoMonthGroup> PhotoGroups { get; set; } = new();
    public string CurrentSort { get; set; } = "DateTakenDesc";
    public int TotalPhotoCount { get; set; }
    
    // Property configuration for quick filters
    public int DayHour { get; set; }
    public int NightHour { get; set; }
    
    // Filter information
    public PhotoFilters? AppliedFilters { get; set; }
    public bool HasFiltersApplied => AppliedFilters?.HasAnyFilters == true;
    
    // Available filter options (for UI)
    public List<CameraOption> AvailableCameras { get; set; } = new();
    public List<string> AvailableConditions { get; set; } = new();
    public List<string> AvailableMoonPhases { get; set; } = new();
    public List<string> AvailablePressureTrends { get; set; } = new();
    public List<string> AvailableWindDirections { get; set; } = new();
    public List<WindDirectionOption> WindDirectionOptions { get; set; } = new();
    
    // Tagging support
    public List<TagInfo> AvailableTags { get; set; } = new();
}

public class CameraOption
{
    public int Id { get; set; }
    public string LocationName { get; set; } = string.Empty;
}

public class PhotoMonthGroup
{
    public string MonthYear { get; set; } = string.Empty; // e.g., "October 2024"
    public List<PropertyPhotoListItemVm> Photos { get; set; } = new();
}

public class PropertyPhotoListItemVm
{
    public int Id { get; set; }
    public string PhotoUrl { get; set; } = string.Empty;
    public DateTime DateTaken { get; set; }
    public DateTime DateUploaded { get; set; }
    public int CameraId { get; set; }
    public string CameraLocationName { get; set; } = string.Empty;
    public List<TagInfo> Tags { get; set; } = new();
    
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

public static class PhotoGroupingExtensions
{
    public static List<PhotoMonthGroup> GroupByMonth(this List<ListPropertyPhotos.PhotoListItem> photos, bool isAscending = false)
    {
        var groups = photos.GroupBy(p => new { p.DateTaken.Year, p.DateTaken.Month });
        
        // Order groups based on sort direction
        var orderedGroups = isAscending 
            ? groups.OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            : groups.OrderByDescending(g => g.Key.Year).ThenByDescending(g => g.Key.Month);
        
        return orderedGroups
            .Select(g => new PhotoMonthGroup
            {
                MonthYear = $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(g.Key.Month)} {g.Key.Year}",
                Photos = g.Select(p => new PropertyPhotoListItemVm
                {
                    Id = p.Id,
                    PhotoUrl = p.PhotoUrl,
                    DateTaken = p.DateTaken,
                    DateUploaded = p.DateUploaded,
                    CameraId = p.CameraId,
                    CameraLocationName = p.CameraLocationName
                }).ToList()
            })
            .ToList();
    }
}

public class TagInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}