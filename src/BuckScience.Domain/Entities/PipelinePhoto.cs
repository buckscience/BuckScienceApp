namespace BuckScience.Domain.Entities;

/// <summary>
/// Photo entity for the Azure-first upload pipeline
/// Stores metadata without original files
/// </summary>
public class PipelinePhoto
{
    protected PipelinePhoto() { } // EF

    public PipelinePhoto(
        string userId,
        int cameraId,
        string contentHash,
        string thumbBlobName,
        string displayBlobName,
        DateTime? takenAtUtc = null,
        decimal? latitude = null,
        decimal? longitude = null)
    {
        SetUserId(userId);
        SetCamera(cameraId);
        SetContentHash(contentHash);
        SetBlobNames(thumbBlobName, displayBlobName);
        TakenAtUtc = takenAtUtc;
        Latitude = latitude;
        Longitude = longitude;
        Status = "processing";
        CreatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public int CameraId { get; private set; }
    public string ContentHash { get; private set; } = string.Empty;
    public string ThumbBlobName { get; private set; } = string.Empty;
    public string DisplayBlobName { get; private set; } = string.Empty;
    public DateTime? TakenAtUtc { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string? WeatherJson { get; private set; }
    public string Status { get; private set; } = "processing";
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    // Behavior methods
    public void SetUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        UserId = userId.Trim();
    }

    public void SetCamera(int cameraId)
    {
        if (cameraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(cameraId));
        CameraId = cameraId;
    }

    public void SetContentHash(string contentHash)
    {
        if (string.IsNullOrWhiteSpace(contentHash))
            throw new ArgumentException("ContentHash is required.", nameof(contentHash));
        ContentHash = contentHash.Trim();
    }

    public void SetBlobNames(string thumbBlobName, string displayBlobName)
    {
        if (string.IsNullOrWhiteSpace(thumbBlobName))
            throw new ArgumentException("ThumbBlobName is required.", nameof(thumbBlobName));
        if (string.IsNullOrWhiteSpace(displayBlobName))
            throw new ArgumentException("DisplayBlobName is required.", nameof(displayBlobName));
        
        ThumbBlobName = thumbBlobName.Trim();
        DisplayBlobName = displayBlobName.Trim();
    }

    public void SetWeatherData(string? weatherJson)
    {
        WeatherJson = weatherJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            throw new ArgumentException("Status is required.", nameof(status));
        Status = status.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkReady(string? weatherJson = null)
    {
        WeatherJson = weatherJson;
        Status = "ready";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = "failed";
        UpdatedAtUtc = DateTime.UtcNow;
    }
}