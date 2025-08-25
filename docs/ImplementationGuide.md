# Trail Camera Photo Processing - Implementation Guide

## Quick Reference

This guide provides specific implementation details to complement the main architecture document. Use this as a technical reference during development.

## Domain Model Extensions

### ProcessingStatus Enum
```csharp
public enum ProcessingStatus
{
    Uploaded = 0,           // Initial state after upload
    Queued = 1,            // Message sent to queue
    ThumbnailProcessing = 2, // Thumbnail generation in progress
    DisplayProcessing = 3,   // Display image generation in progress
    Completed = 4,          // Both thumbnail and display completed
    Failed = 5,             // Unrecoverable failure
    RequiresRetry = 6       // Temporary failure, retry needed
}
```

### Photo Entity Extensions
**Recommended properties to add to existing Photo entity:**

```csharp
// File metadata
public long OriginalFileSizeBytes { get; private set; }
public string? OriginalFileName { get; private set; }
public string OriginalContentType { get; private set; } = string.Empty;

// Processing tracking
public ProcessingStatus ProcessingStatus { get; private set; } = ProcessingStatus.Uploaded;
public int ProcessingRetryCount { get; private set; }
public string? ProcessingErrorMessage { get; private set; }
public DateTime? ProcessingStartedAt { get; private set; }
public DateTime? ProcessingCompletedAt { get; private set; }

// EXIF extracted data
public DateTime? ExifDateTaken { get; private set; }
public string? CameraExifMake { get; private set; }
public string? CameraExifModel { get; private set; }
public decimal? ExifLatitude { get; private set; }
public decimal? ExifLongitude { get; private set; }
public int? ExifOrientation { get; private set; }

// Processed images
public string? ThumbnailBlobUrl { get; private set; }
public string? DisplayBlobUrl { get; private set; }
public long ThumbnailSizeBytes { get; private set; }
public long DisplaySizeBytes { get; private set; }

// Domain methods for trail camera processing
public void SetProcessingStatus(ProcessingStatus status, string? errorMessage = null)
{
    ProcessingStatus = status;
    ProcessingErrorMessage = errorMessage;
    
    if (status == ProcessingStatus.ThumbnailProcessing || status == ProcessingStatus.DisplayProcessing)
    {
        ProcessingStartedAt ??= DateTime.UtcNow;
    }
    else if (status == ProcessingStatus.Completed)
    {
        ProcessingCompletedAt = DateTime.UtcNow;
    }
}

public void SetExifData(DateTime? dateTaken, string? make, string? model, 
                       decimal? latitude, decimal? longitude, int? orientation)
{
    ExifDateTaken = dateTaken;
    CameraExifMake = make;
    CameraExifModel = model;
    ExifLatitude = latitude;
    ExifLongitude = longitude;
    ExifOrientation = orientation;
    
    // Update DateTaken if EXIF provides more accurate time
    if (dateTaken.HasValue && dateTaken.Value != default)
    {
        SetDateTaken(dateTaken.Value);
    }
}

public void SetProcessedImageUrls(string? thumbnailUrl, string? displayUrl, 
                                 long thumbnailBytes, long displayBytes)
{
    ThumbnailBlobUrl = thumbnailUrl;
    DisplayBlobUrl = displayUrl;
    ThumbnailSizeBytes = thumbnailBytes;
    DisplaySizeBytes = displayBytes;
}

public void IncrementRetryCount()
{
    ProcessingRetryCount++;
}
```

## Message Queue Integration

### Service Bus Message DTOs

```csharp
public record PhotoProcessingMessage
{
    public Guid PhotoId { get; init; }
    public int CameraId { get; init; }
    public string OriginalBlobUrl { get; init; } = string.Empty;
    public ProcessingType ProcessingType { get; init; }
    public int Priority { get; init; } = 1;
    public DateTime UploadTimestamp { get; init; }
    public int RetryCount { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public enum ProcessingType
{
    Thumbnail = 1,
    Display = 2,
    Both = 3
}
```

### Queue Configuration

**Azure Service Bus Topic Setup**:
```csharp
// appsettings.json configuration
{
  "ServiceBus": {
    "ConnectionString": "Endpoint=sb://...",
    "TopicName": "photo-processing",
    "Subscriptions": {
      "ThumbnailProcessing": {
        "MaxDeliveryCount": 3,
        "LockDuration": "00:05:00",
        "TimeToLive": "01:00:00",
        "Filter": "ProcessingType = 'Thumbnail' OR ProcessingType = 'Both'"
      },
      "DisplayProcessing": {
        "MaxDeliveryCount": 5,
        "LockDuration": "00:10:00",
        "TimeToLive": "1.00:00:00",
        "Filter": "ProcessingType = 'Display' OR ProcessingType = 'Both'"
      }
    }
  }
}
```

## Azure Function Implementation Patterns

### Function App Structure
```
TrailCameraProcessor/
├── Functions/
│   ├── ThumbnailProcessor.cs
│   ├── DisplayProcessor.cs
│   └── CleanupProcessor.cs
├── Services/
│   ├── IImageProcessor.cs
│   ├── ImageProcessor.cs
│   ├── IExifExtractor.cs
│   ├── ExifExtractor.cs
│   └── IBlobStorageService.cs
├── Models/
│   └── ProcessingModels.cs
└── Extensions/
    └── ServiceCollectionExtensions.cs
```

### EXIF Extraction Service Interface
```csharp
public interface IExifExtractor
{
    Task<ExifData> ExtractAsync(Stream imageStream, string fileName);
}

public record ExifData
{
    public DateTime? DateTaken { get; init; }
    public string? CameraMake { get; init; }
    public string? CameraModel { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public int? Orientation { get; init; }
    public bool IsValid { get; init; }
    public string? ErrorMessage { get; init; }
}
```

### WebP Processing Service Interface
```csharp
public interface IImageProcessor
{
    Task<ProcessedImage> CreateThumbnailAsync(Stream sourceStream, int targetSizeKB = 15);
    Task<ProcessedImage> CreateDisplayImageAsync(Stream sourceStream, int targetSizeKB = 300);
}

public record ProcessedImage
{
    public byte[] ImageData { get; init; } = Array.Empty<byte>();
    public long SizeBytes { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public string Format { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
}
```

## Database Migration Script

### SQL Server Migration for Photo Entity Extensions
```sql
-- Add new columns to Photos table
ALTER TABLE Photos ADD
    OriginalFileSizeBytes BIGINT NULL,
    OriginalFileName NVARCHAR(500) NULL,
    OriginalContentType NVARCHAR(100) NULL,
    ProcessingStatus INT NOT NULL DEFAULT 0,
    ProcessingRetryCount INT NOT NULL DEFAULT 0,
    ProcessingErrorMessage NVARCHAR(MAX) NULL,
    ProcessingStartedAt DATETIME2 NULL,
    ProcessingCompletedAt DATETIME2 NULL,
    ExifDateTaken DATETIME2 NULL,
    CameraExifMake NVARCHAR(100) NULL,
    CameraExifModel NVARCHAR(100) NULL,
    ExifLatitude DECIMAL(10,8) NULL,
    ExifLongitude DECIMAL(11,8) NULL,
    ExifOrientation INT NULL,
    ThumbnailBlobUrl NVARCHAR(2048) NULL,
    DisplayBlobUrl NVARCHAR(2048) NULL,
    ThumbnailSizeBytes BIGINT NOT NULL DEFAULT 0,
    DisplaySizeBytes BIGINT NOT NULL DEFAULT 0;

-- Add indexes for common queries
CREATE INDEX IX_Photos_ProcessingStatus ON Photos (ProcessingStatus);
CREATE INDEX IX_Photos_ExifDateTaken ON Photos (ExifDateTaken);
CREATE INDEX IX_Photos_ProcessingStartedAt ON Photos (ProcessingStartedAt);
CREATE INDEX IX_Photos_CameraId_ProcessingStatus ON Photos (CameraId, ProcessingStatus);
```

## API Integration Points

### Photo Upload API Extension
```csharp
[Route("api/cameras/{cameraId}/photos")]
public class TrailCameraPhotosController : ControllerBase
{
    [HttpPost("upload")]
    public async Task<IActionResult> UploadTrailCameraPhoto(
        int cameraId,
        [FromForm] TrailCameraUploadRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Validate camera ownership and file
        // 2. Upload to temporary blob storage
        // 3. Create photo record with metadata
        // 4. Publish to Service Bus queue
        // 5. Return immediate response with tracking ID
    }
    
    [HttpGet("{photoId}/status")]
    public async Task<IActionResult> GetProcessingStatus(int photoId)
    {
        // Return current processing status and URLs if available
    }
}

public record TrailCameraUploadRequest
{
    public IFormFile PhotoFile { get; init; } = default!;
    public string? FileName { get; init; }
    public DateTime? CaptureTime { get; init; }
    public bool GenerateThumbnailOnly { get; init; } = false;
}
```

## Configuration & Environment Setup

### Required NuGet Packages
```xml
<!-- For existing projects -->
<PackageReference Include="Azure.Storage.Blobs" Version="12.19.1" />
<PackageReference Include="Azure.Messaging.ServiceBus" Version="7.17.0" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.0.2" />
<PackageReference Include="SixLabors.ImageSharp.Web" Version="3.0.1" />
<PackageReference Include="MetadataExtractor" Version="2.8.1" />

<!-- For Azure Functions -->
<PackageReference Include="Microsoft.Azure.Functions.Worker" Version="1.19.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.ServiceBus" Version="5.13.0" />
<PackageReference Include="Microsoft.Azure.Functions.Worker.Extensions.Storage.Blobs" Version="6.2.0" />
```

### Environment Variables
```bash
# Azure Storage
AZURE_STORAGE_CONNECTION_STRING="DefaultEndpointsProtocol=https;..."
TEMP_STORAGE_CONTAINER="trail-camera-originals"
PROCESSED_STORAGE_CONTAINER="trail-camera-processed"

# Service Bus
AZURE_SERVICE_BUS_CONNECTION_STRING="Endpoint=sb://..."
PHOTO_PROCESSING_TOPIC="photo-processing"

# Processing Configuration
THUMBNAIL_TARGET_SIZE_KB=15
DISPLAY_TARGET_SIZE_KB=300
MAX_PROCESSING_RETRIES=3
PROCESSING_TIMEOUT_MINUTES=10

# Database
PHOTO_DATABASE_CONNECTION_STRING="Server=...;Database=...;"
```

## Monitoring & Alerts Setup

### Application Insights Custom Events
```csharp
// Track processing metrics
telemetryClient.TrackEvent("PhotoProcessingStarted", new Dictionary<string, string>
{
    ["PhotoId"] = photoId.ToString(),
    ["ProcessingType"] = processingType.ToString(),
    ["FileSizeBytes"] = originalSize.ToString()
});

telemetryClient.TrackMetric("PhotoProcessingDuration", duration.TotalSeconds, new Dictionary<string, string>
{
    ["ProcessingType"] = processingType.ToString(),
    ["Success"] = success.ToString()
});
```

### Recommended Alerts
1. **Queue Depth Alert**: > 100 messages in thumbnail queue for > 5 minutes
2. **Processing Failure Rate**: > 10% failures in last 15 minutes
3. **Storage Quota**: > 80% of storage account capacity used
4. **Function App Errors**: > 5 errors per minute in Azure Functions
5. **Processing Time**: Average processing time > 120 seconds for thumbnails

## Testing Strategy

### Unit Tests Focus Areas
- EXIF extraction accuracy with various camera formats
- WebP conversion quality and file size achievement
- Message queue serialization/deserialization
- Error handling and retry logic
- Processing status state transitions

### Integration Tests
- End-to-end photo upload → processing → storage workflow
- Azure Service Bus message handling
- Blob storage operations with proper cleanup
- Database consistency during processing failures

### Performance Tests
- Concurrent processing with multiple photos
- Memory usage during large image processing
- Queue throughput under high load
- Storage account bandwidth limits

This implementation guide provides the specific technical details needed to build the trail camera photo processing system according to the architecture specifications.