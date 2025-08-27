using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure;
using Microsoft.Extensions.Logging;

namespace BuckScience.Application.Photos;

public interface IBlobStorageService
{
    Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;
    private const string ContainerName = "photos";

    public BlobStorageService(string connectionString, ILogger<BlobStorageService> logger)
    {
        var options = new BlobClientOptions()
        {
            Retry = {
                MaxRetries = 2,
                Delay = TimeSpan.FromSeconds(1),
                MaxDelay = TimeSpan.FromSeconds(5)
            }
        };
        
        _blobServiceClient = new BlobServiceClient(connectionString, options);
        _logger = logger;
    }

    public async Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Starting Azure Blob Storage upload for photo {PhotoId}", photoId);
            
            // Get or create the container
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

            // Generate a unique blob name with URL-encoded filename
            var sanitizedFileName = SanitizeFileName(fileName);
            var blobName = $"{Guid.NewGuid()}_{sanitizedFileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set metadata for the blob
            var metadata = new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "cameraId", cameraId.ToString() },
                { "photoId", photoId.ToString() },
                { "originalFileName", fileName },
                { "uploadedAt", DateTime.UtcNow.ToString("O") }
            };

            // Upload the blob with metadata
            content.Position = 0;
            await blobClient.UploadAsync(content, new BlobUploadOptions
            {
                Metadata = metadata,
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = GetContentType(fileName)
                }
            }, ct);

            _logger.LogInformation("Successfully uploaded photo {PhotoId} to Azure Blob Storage", photoId);
            return blobClient.Uri.ToString();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Blob Storage request failed for photo {PhotoId}: {Error}", photoId, ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading photo {PhotoId} to Azure Blob Storage", photoId);
            throw;
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "photo";

        // Remove path information and get just the filename
        fileName = Path.GetFileName(fileName);
        
        // Replace spaces and special characters that are problematic in URLs
        var sanitized = fileName
            .Replace(" ", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace("&", "and")
            .Replace("%", "pct")
            .Replace("#", "")
            .Replace("?", "")
            .Replace("!", "")
            .Replace("@", "at")
            .Replace("$", "")
            .Replace("^", "")
            .Replace("+", "plus")
            .Replace("=", "")
            .Replace("|", "")
            .Replace("\\", "")
            .Replace("/", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("\"", "")
            .Replace("'", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace(",", "");

        // Remove multiple consecutive underscores
        while (sanitized.Contains("__"))
        {
            sanitized = sanitized.Replace("__", "_");
        }
        
        // Trim underscores from start and end
        sanitized = sanitized.Trim('_');
        
        // If the result is empty or too short, provide a default
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized.Length < 3)
        {
            sanitized = "photo";
        }
        
        return sanitized;
    }
}