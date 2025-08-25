using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.API.Controllers;

[ApiController]
[Route("[controller]")]
public class UploadController : ControllerBase
{
    private readonly BlobServiceClient _blobServiceClient;

    public UploadController(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    /// <summary>
    /// Generate SAS URL for uploading a blob to the photos container
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="blobName">Blob name (e.g., {userId}/{hash}_thumb.webp)</param>
    /// <returns>SAS URL for PUT upload</returns>
    [HttpPost("sas")]
    public IActionResult GenerateSasUrl([FromBody] GenerateSasRequest request)
    {
        try
        {
            // Get container client for "photos"
            var containerClient = _blobServiceClient.GetBlobContainerClient("photos");
            
            // Get blob client
            var blobClient = containerClient.GetBlobClient(request.BlobName);

            // Check if the blob client can generate SAS tokens
            if (!blobClient.CanGenerateSasUri)
            {
                return BadRequest("Blob client cannot generate SAS URIs. Ensure you're using account key authentication.");
            }

            // Generate SAS token with Create and Write permissions, valid for 15 minutes
            var sasUri = blobClient.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(15));

            return Ok(new { SasUrl = sasUri.ToString() });
        }
        catch (Exception ex)
        {
            return BadRequest($"Failed to generate SAS URL: {ex.Message}");
        }
    }
}

public class GenerateSasRequest
{
    public string UserId { get; set; } = string.Empty;
    public string BlobName { get; set; } = string.Empty;
}