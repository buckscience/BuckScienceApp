using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace BuckScience.Application.Photos;

public class HybridStorageService : IBlobStorageService
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILocalFileStorageService _localFileStorageService;
    private readonly ILogger<HybridStorageService> _logger;

    public HybridStorageService(
        IBlobStorageService blobStorageService,
        ILocalFileStorageService localFileStorageService,
        ILogger<HybridStorageService> logger)
    {
        _blobStorageService = blobStorageService;
        _localFileStorageService = localFileStorageService;
        _logger = logger;
    }

    public async Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default)
    {
        try
        {
            // Try Azure Blob Storage first
            _logger.LogInformation("Attempting to upload photo to Azure Blob Storage");
            return await _blobStorageService.UploadPhotoAsync(content, fileName, userId, cameraId, photoId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Blob Storage upload failed, falling back to local file storage");
            
            // Fall back to local file storage
            return await _localFileStorageService.UploadPhotoAsync(content, fileName, userId, cameraId, photoId, ct);
        }
    }
}