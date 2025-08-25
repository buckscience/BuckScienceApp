# Trail Camera Photo Processing Pipeline

## Overview

This document provides a comprehensive guide to the trail camera photo processing pipeline implemented in the BuckScience app. The system follows the workflow specified in the problem statement diagram, efficiently handling large trail camera photos (5-40 MP) by creating optimized WebP versions and immediately deleting originals.

## Architecture Flow

```
┌───────────────┐         ┌────────────────┐         ┌────────────────┐
│  Trail Camera │         │  Temp Storage  │         │  Processing    │
│  Photos       ├────────►│  (Originals)   ├────────►│  Function      │
│  (5-40 MP)    │         │  Short-lived   │         │                │
└───────────────┘         └────────────────┘         └───────┬────────┘
                                                             │
                                                             ▼
                                     ┌──────────────────────┐ │ ┌────────────────┐
                                     │                      │ │ │                │
                                     │  DELETE ORIGINALS    │◄┘ │  WebP Display  │
                                     │  Immediately         │   │  (300KB)       │
                                     │                      │   │                │
                                     └──────────────────────┘   └───────┬────────┘
                                                                        │
                                                                        │
                                                                        ▼
                                                              ┌────────────────┐
                                                              │                │
                                                              │  WebP Thumbnail│
                                                              │  (15KB)        │
                                                              │                │
                                                              └────────────────┘
```

## Key Components

### 1. Application Services

#### `IPhotoProcessingService`
- **Purpose**: Core interface for photo processing
- **Implementation**: `PhotoProcessingService`
- **Key Method**: `ProcessPhotoAsync(string tempFilePath, string baseFileName, CancellationToken ct)`

**Processing Steps:**
1. Load original image using ImageSharp
2. Create display version (max 1920x1080, WebP quality 85)
3. Create thumbnail version (max 300x200, WebP quality 75)
4. Store both processed images
5. Return processing results with URLs and file sizes
6. Automatically delete original temp file

#### `IFileStorageService`
- **Purpose**: Abstraction for file storage operations
- **Implementation**: `LocalFileStorageService`
- **Storage Locations**:
  - Temp files: `{ContentRootPath}/temp/`
  - Processed photos: `{WebRootPath}/photos/`

### 2. Domain Model Enhancements

#### Updated Photo Entity
```csharp
public class Photo
{
    // Existing properties
    public string PhotoUrl { get; private set; }        // Display image URL
    
    // New properties for processing pipeline
    public string? ThumbnailUrl { get; private set; }   // Thumbnail image URL
    public long? FileSizeBytes { get; private set; }    // Display image size
    public long? ThumbnailSizeBytes { get; private set; } // Thumbnail size
    
    // New constructor for processed photos
    public Photo(int cameraId, string displayUrl, string? thumbnailUrl, 
                DateTime dateTaken, long? displaySizeBytes = null, 
                long? thumbnailSizeBytes = null, DateTime? dateUploaded = null)
}
```

### 3. Controller Integration

#### CamerasController Upload Handler
- **Endpoint**: `POST /cameras/{id}/photos/upload`
- **Validation**: File type (JPEG, PNG, TIFF), size (max 50MB)
- **Process Flow**:
  1. Validate file and camera ownership
  2. Store file temporarily via `IFileStorageService`
  3. Process photo via `IPhotoProcessingService`
  4. Create Photo entity with processed URLs
  5. Save to database
  6. Handle errors and cleanup

## Configuration Options

### Image Processing Settings

**Display Image Settings** (PhotoProcessingService.cs):
```csharp
private const int DisplayMaxWidth = 1920;    // Max width for display images
private const int DisplayMaxHeight = 1080;   // Max height for display images
private const int DisplayQuality = 85;       // WebP quality (targets ~300KB)
```

**Thumbnail Settings**:
```csharp
private const int ThumbnailMaxWidth = 300;   // Max width for thumbnails
private const int ThumbnailMaxHeight = 200;  // Max height for thumbnails
private const int ThumbnailQuality = 75;     // WebP quality (targets ~15KB)
```

### File Storage Configuration

**Local Storage Paths** (LocalFileStorageService.cs):
```csharp
private const string TempFolder = "temp";     // Temporary storage folder
private const string PhotosFolder = "photos"; // Processed photos folder
```

## Deployment Considerations

### 1. Directory Setup
Ensure these directories exist and have proper permissions:
- `{ContentRootPath}/temp/` - For temporary file storage
- `{WebRootPath}/photos/` - For processed photos (publicly accessible)

### 2. Database Migration
Run the migration to add new Photo properties:
```bash
dotnet ef database update
```

### 3. Storage Scaling Options

**Current Implementation**: Local file storage
**Recommended for Production**: Azure Blob Storage or AWS S3

#### Implementing Cloud Storage
Create a new implementation of `IFileStorageService`:

```csharp
public class AzureBlobStorageService : IFileStorageService
{
    // Implement Azure Blob Storage operations
    // Store temp files in a short-lived container
    // Store processed photos in a public container
    // Set lifecycle policies for automatic temp cleanup
}
```

## Performance Considerations

### 1. File Size Optimization
- **Display images**: Target ~300KB through quality adjustment
- **Thumbnails**: Target ~15KB for fast loading
- **Original deletion**: Immediate cleanup saves storage costs

### 2. Processing Time
- Typical trail camera photo (8-12MP): 2-5 seconds processing time
- Processing runs asynchronously after upload
- User receives immediate feedback when upload completes

### 3. Concurrent Processing
Current implementation processes photos synchronously. For high-volume scenarios, consider:

```csharp
// Background processing with message queues
public interface IPhotoProcessingQueue
{
    Task QueuePhotoForProcessingAsync(int photoId, string tempFilePath);
}
```

## Extension Points

### 1. Advanced Image Analysis
```csharp
public interface IPhotoAnalysisService
{
    Task<PhotoAnalysisResult> AnalyzePhotoAsync(string imagePath);
}

public record PhotoAnalysisResult(
    DateTime? ExifDateTaken,
    GpsCoordinates? GpsLocation,
    List<DetectedAnimal> DetectedAnimals,
    WeatherConditions? EstimatedWeather
);
```

### 2. Format Support Extension
Current: JPEG, PNG, TIFF
Potential additions: RAW formats (CR2, NEF, ARW)

### 3. Processing Pipeline Customization
```csharp
public class TrailCameraProcessingOptions
{
    public int DisplayMaxWidth { get; set; } = 1920;
    public int DisplayMaxHeight { get; set; } = 1080;
    public int DisplayQuality { get; set; } = 85;
    public int ThumbnailMaxWidth { get; set; } = 300;
    public int ThumbnailMaxHeight { get; set; } = 200;
    public int ThumbnailQuality { get; set; } = 75;
    public bool EnableWatermark { get; set; } = false;
    public bool PreserveExifData { get; set; } = true;
}
```

## Error Handling

### Common Scenarios
1. **Unsupported file format**: Returns 400 with clear message
2. **File too large**: Returns 500 with size limit information
3. **Corrupted image**: Returns 500 with processing error details
4. **Storage full**: Returns 500 with storage error information
5. **Camera ownership**: Returns 403 or 404 based on security policy

### Retry Logic
```csharp
// Implement retry logic for transient failures
public async Task<PhotoProcessingResult> ProcessPhotoWithRetryAsync(
    string tempFilePath, string baseFileName, int maxRetries = 3)
{
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            return await ProcessPhotoAsync(tempFilePath, baseFileName);
        }
        catch (Exception ex) when (IsRetriableException(ex) && attempt < maxRetries)
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt))); // Exponential backoff
        }
    }
    throw new PhotoProcessingException("Failed after maximum retry attempts");
}
```

## Testing Strategy

### Unit Tests
- File storage operations
- Image processing configuration
- Error handling scenarios

### Integration Tests
- End-to-end upload processing
- Database operations
- File system operations

### Load Testing
- Concurrent upload processing
- Large file handling
- Storage performance under load

## Monitoring and Logging

### Key Metrics
- Processing time per photo
- File size reduction ratios
- Error rates by file type
- Storage utilization

### Logging Examples
```csharp
_logger.LogInformation("Photo processing started: {OriginalSize}MB", 
    originalSizeBytes / 1024.0 / 1024.0);
    
_logger.LogInformation("Photo processing completed: Display={DisplaySize}KB, Thumbnail={ThumbnailSize}KB, Reduction={ReductionPercent}%",
    displaySizeBytes / 1024, thumbnailSizeBytes / 1024, reductionPercent);
```

## Security Considerations

### 1. File Validation
- Validate file headers, not just extensions
- Implement virus scanning for uploaded files
- Limit concurrent uploads per user

### 2. Storage Security
- Ensure temp directories are not web-accessible
- Implement proper access controls on photo directories
- Consider encryption for sensitive photos

### 3. Access Control
- Verify camera ownership before processing
- Implement rate limiting on upload endpoints
- Log all photo processing activities

This implementation provides a solid foundation for trail camera photo processing that can be extended and scaled as needed.