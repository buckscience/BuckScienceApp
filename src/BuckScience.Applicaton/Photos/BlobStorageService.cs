using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace BuckScience.Application.Photos;

public interface IBlobStorageService
{
    Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private const string ContainerName = "photos";

    public BlobStorageService(string connectionString)
    {
        _blobServiceClient = new BlobServiceClient(connectionString);
    }

    public async Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default)
    {
        // Get or create the container
        var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);

        // Generate a unique blob name
        var blobName = $"{Guid.NewGuid()}_{fileName}";
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

        return blobClient.Uri.ToString();
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
}