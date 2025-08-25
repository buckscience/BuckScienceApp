namespace BuckScience.Application.Photos.Messages;

/// <summary>
/// Queue message for background photo processing
/// </summary>
public class PhotoIngestMessage
{
    /// <summary>
    /// ID of the photo record to process
    /// </summary>
    public int PhotoId { get; set; }

    /// <summary>
    /// User identifier (for logging/debugging)
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Camera that took the photo
    /// </summary>
    public int CameraId { get; set; }

    /// <summary>
    /// Content hash (for logging/debugging)
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// When the photo was taken (UTC) - used for weather data lookup
    /// </summary>
    public DateTime? TakenAtUtc { get; set; }

    /// <summary>
    /// GPS coordinates for weather lookup (optional)
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// GPS coordinates for weather lookup (optional)
    /// </summary>
    public decimal? Longitude { get; set; }
}