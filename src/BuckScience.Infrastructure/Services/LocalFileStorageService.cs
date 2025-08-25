using BuckScience.Application.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace BuckScience.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private const string TempFolder = "temp";
    private const string PhotosFolder = "photos";

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string> StoreTempFileAsync(IFormFile file, CancellationToken ct = default)
    {
        var tempDir = Path.Combine(_environment.ContentRootPath, TempFolder);
        Directory.CreateDirectory(tempDir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(tempDir, fileName);

        using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream, ct);

        return filePath;
    }

    public async Task<string> StoreProcessedPhotoAsync(byte[] imageData, string fileName, CancellationToken ct = default)
    {
        var photosDir = Path.Combine(_environment.WebRootPath, PhotosFolder);
        Directory.CreateDirectory(photosDir);

        var filePath = Path.Combine(photosDir, fileName);
        await File.WriteAllBytesAsync(filePath, imageData, ct);

        // Return the public URL path
        return $"/{PhotosFolder}/{fileName}";
    }

    public Task DeleteTempFileAsync(string filePath, CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Log the error but don't throw - temp file cleanup is best effort
        }
        
        return Task.CompletedTask;
    }

    public Task DeleteProcessedPhotoAsync(string photoUrl, CancellationToken ct = default)
    {
        try
        {
            // Convert URL back to file path
            var fileName = Path.GetFileName(photoUrl);
            var filePath = Path.Combine(_environment.WebRootPath, PhotosFolder, fileName);
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Log the error but don't throw - photo cleanup is best effort
        }
        
        return Task.CompletedTask;
    }
}