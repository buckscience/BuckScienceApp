using BuckScience.Application.Abstractions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace BuckScience.Application.Photos;

public class PhotoProcessingService : IPhotoProcessingService
{
    private readonly IFileStorageService _fileStorage;
    
    // Target sizes based on the problem statement diagram
    private const int DisplayMaxWidth = 1920;
    private const int DisplayMaxHeight = 1080;
    private const int ThumbnailMaxWidth = 300;
    private const int ThumbnailMaxHeight = 200;
    private const int DisplayQuality = 85;  // WebP quality for ~300KB target
    private const int ThumbnailQuality = 75;  // WebP quality for ~15KB target

    public PhotoProcessingService(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
    }

    public async Task<PhotoProcessingResult> ProcessPhotoAsync(string tempFilePath, string baseFileName, CancellationToken ct = default)
    {
        if (!File.Exists(tempFilePath))
            throw new FileNotFoundException($"Temporary file not found: {tempFilePath}");

        var originalFileInfo = new FileInfo(tempFilePath);
        var originalSizeBytes = originalFileInfo.Length;

        try
        {
            // Load the original image
            using var image = await Image.LoadAsync(tempFilePath, ct);
            
            // Generate display version
            var displayImageData = await CreateDisplayVersionAsync(image, ct);
            var displayFileName = $"{baseFileName}_display.webp";
            var displayUrl = await _fileStorage.StoreProcessedPhotoAsync(displayImageData, displayFileName, ct);

            // Generate thumbnail version
            var thumbnailImageData = await CreateThumbnailVersionAsync(image, ct);
            var thumbnailFileName = $"{baseFileName}_thumb.webp";
            var thumbnailUrl = await _fileStorage.StoreProcessedPhotoAsync(thumbnailImageData, thumbnailFileName, ct);

            return new PhotoProcessingResult(
                DisplayImageUrl: displayUrl,
                ThumbnailImageUrl: thumbnailUrl,
                OriginalFileSizeBytes: originalSizeBytes,
                DisplayFileSizeBytes: displayImageData.Length,
                ThumbnailFileSizeBytes: thumbnailImageData.Length
            );
        }
        finally
        {
            // Delete the original temporary file immediately after processing
            await _fileStorage.DeleteTempFileAsync(tempFilePath, ct);
        }
    }

    private async Task<byte[]> CreateDisplayVersionAsync(Image image, CancellationToken ct)
    {
        // Create a copy of the image for processing
        var displayImage = image.CloneAs<Rgba32>();
        
        try
        {
            // Resize to display dimensions while maintaining aspect ratio
            displayImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(DisplayMaxWidth, DisplayMaxHeight),
                Mode = ResizeMode.Max
            }));

            // Save as WebP with target quality
            using var stream = new MemoryStream();
            var encoder = new WebpEncoder { Quality = DisplayQuality };
            await displayImage.SaveAsync(stream, encoder, ct);
            
            return stream.ToArray();
        }
        finally
        {
            displayImage.Dispose();
        }
    }

    private async Task<byte[]> CreateThumbnailVersionAsync(Image image, CancellationToken ct)
    {
        // Create a copy of the image for processing
        var thumbnailImage = image.CloneAs<Rgba32>();
        
        try
        {
            // Resize to thumbnail dimensions while maintaining aspect ratio
            thumbnailImage.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(ThumbnailMaxWidth, ThumbnailMaxHeight),
                Mode = ResizeMode.Max
            }));

            // Save as WebP with thumbnail quality
            using var stream = new MemoryStream();
            var encoder = new WebpEncoder { Quality = ThumbnailQuality };
            await thumbnailImage.SaveAsync(stream, encoder, ct);
            
            return stream.ToArray();
        }
        finally
        {
            thumbnailImage.Dispose();
        }
    }
}