using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace BuckScience.Application.Photos;

public class HybridStorageService : IBlobStorageService
{
    private readonly BlobStorageService _blobStorageService;
    private readonly ILocalFileStorageService _localFileStorageService;
    private readonly ILogger<HybridStorageService> _logger;
    private bool? _azureAvailable = null;

    public HybridStorageService(
        BlobStorageService blobStorageService,
        ILocalFileStorageService localFileStorageService,
        ILogger<HybridStorageService> logger)
    {
        _blobStorageService = blobStorageService;
        _localFileStorageService = localFileStorageService;
        _logger = logger;
    }

    public async Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default)
    {
        // Test Azure connectivity once and cache the result
        if (_azureAvailable == null)
        {
            _logger.LogInformation("Testing Azure Storage connectivity for first upload");
            _azureAvailable = await _blobStorageService.TestConnectivityAsync(ct);
            
            if (_azureAvailable == false)
            {
                _logger.LogWarning("Azure Storage is not available, all uploads will use local storage");
            }
        }

        // If Azure is known to be unavailable, go straight to local storage
        if (_azureAvailable == false)
        {
            _logger.LogInformation("Using local file storage for photo {PhotoId} (Azure unavailable)", photoId);
            return await _localFileStorageService.UploadPhotoAsync(content, fileName, userId, cameraId, photoId, ct);
        }

        try
        {
            // Try Azure Blob Storage
            _logger.LogInformation("Attempting to upload photo {PhotoId} to Azure Blob Storage", photoId);
            return await _blobStorageService.UploadPhotoAsync(content, fileName, userId, cameraId, photoId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Azure Blob Storage upload failed for photo {PhotoId}, falling back to local file storage", photoId);
            
            // Mark Azure as unavailable for future uploads
            _azureAvailable = false;
            
            // Fall back to local file storage
            return await _localFileStorageService.UploadPhotoAsync(content, fileName, userId, cameraId, photoId, ct);
        }
    }
}