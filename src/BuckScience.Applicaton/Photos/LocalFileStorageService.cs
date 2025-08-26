namespace BuckScience.Application.Photos;

public interface ILocalFileStorageService
{
    Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default);
}

public class LocalFileStorageService : ILocalFileStorageService
{
    private readonly string _uploadPath;

    public LocalFileStorageService(string uploadPath)
    {
        _uploadPath = uploadPath;
    }

    public async Task<string> UploadPhotoAsync(Stream content, string fileName, int userId, int cameraId, int photoId, CancellationToken ct = default)
    {
        // Create the uploads directory if it doesn't exist
        var uploadsDir = Path.Combine(_uploadPath, "uploads");
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        // Generate a unique filename
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(uploadsDir, uniqueFileName);

        // Save the file
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            content.Position = 0;
            await content.CopyToAsync(fileStream, ct);
        }

        // Return the relative URL path (compatible with existing photo display logic)
        return $"/uploads/{uniqueFileName}";
    }
}