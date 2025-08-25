using Microsoft.AspNetCore.Http;

namespace BuckScience.Application.Abstractions;

public interface IFileStorageService
{
    /// <summary>
    /// Stores a file temporarily and returns the temporary file path
    /// </summary>
    Task<string> StoreTempFileAsync(IFormFile file, CancellationToken ct = default);
    
    /// <summary>
    /// Stores processed photo and returns the public URL
    /// </summary>
    Task<string> StoreProcessedPhotoAsync(byte[] imageData, string fileName, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a temporary file
    /// </summary>
    Task DeleteTempFileAsync(string filePath, CancellationToken ct = default);
    
    /// <summary>
    /// Deletes a processed photo
    /// </summary>
    Task DeleteProcessedPhotoAsync(string photoUrl, CancellationToken ct = default);
}