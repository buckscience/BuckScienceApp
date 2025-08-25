namespace BuckScience.Application.Abstractions;

public interface IPhotoProcessingService
{
    /// <summary>
    /// Processes an uploaded photo according to the trail camera workflow:
    /// 1. Creates WebP display version (~300KB)
    /// 2. Creates WebP thumbnail (~15KB)  
    /// 3. Returns URLs for the processed images
    /// </summary>
    Task<PhotoProcessingResult> ProcessPhotoAsync(string tempFilePath, string baseFileName, CancellationToken ct = default);
}

public record PhotoProcessingResult(
    string DisplayImageUrl,
    string ThumbnailImageUrl,
    long OriginalFileSizeBytes,
    long DisplayFileSizeBytes,
    long ThumbnailFileSizeBytes
);