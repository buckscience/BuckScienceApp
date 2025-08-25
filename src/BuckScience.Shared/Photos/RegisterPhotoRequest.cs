namespace BuckScience.Shared.Photos;

/// <summary>
/// Request DTO for registering a photo after it has been uploaded to blob storage
/// </summary>
public class RegisterPhotoRequest
{
    /// <summary>
    /// User identifier who owns the photo
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Camera that took the photo
    /// </summary>
    public int CameraId { get; set; }

    /// <summary>
    /// SHA-256 hash of the original image bytes (computed client-side)
    /// </summary>
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>
    /// Blob name for the thumbnail: {userId}/{hash}_thumb.webp
    /// </summary>
    public string ThumbBlobName { get; set; } = string.Empty;

    /// <summary>
    /// Blob name for the display image: {userId}/{hash}_1200x932.webp
    /// </summary>
    public string DisplayBlobName { get; set; } = string.Empty;

    /// <summary>
    /// Optional: When the photo was taken (UTC)
    /// </summary>
    public DateTime? TakenAtUtc { get; set; }

    /// <summary>
    /// Optional: GPS latitude where photo was taken
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Optional: GPS longitude where photo was taken
    /// </summary>
    public decimal? Longitude { get; set; }
}