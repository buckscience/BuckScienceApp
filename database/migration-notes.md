# Azure Photo Pipeline Database Migration

This migration adds Azure pipeline support columns to the existing Photos table.

## What this migration does

1. **Adds new optional columns** to the existing `Photos` table:
   - `UserId` - Azure user identifier (nvarchar(450))
   - `ContentHash` - SHA-256 hash for deduplication (nvarchar(64))
   - `ThumbBlobName` - Thumbnail blob name (nvarchar(500))
   - `DisplayBlobName` - Display image blob name (nvarchar(500))
   - `TakenAtUtc` - UTC timestamp when photo was taken (datetime2)
   - `Latitude` - GPS latitude (decimal(10,8))
   - `Longitude` - GPS longitude (decimal(11,8))
   - `WeatherJson` - Cached weather data (nvarchar(max))
   - `Status` - Processing status (nvarchar(50))

2. **Adds indexes** for performance:
   - `IX_Photos_UserId`
   - `IX_Photos_ContentHash`
   - `IX_Photos_Status`
   - `IX_Photos_UserId_CameraId_ContentHash` (unique, filtered)

## Backward Compatibility

- **All new columns are optional** - existing Photos records continue to work unchanged
- **Traditional photo workflow** uses the existing required columns (PhotoUrl, DateTaken, DateUploaded, CameraId, WeatherId)
- **Azure pipeline workflow** uses the new optional columns for enhanced functionality

## Usage

### Traditional workflow (unchanged)
```csharp
var photo = new Photo(cameraId, photoUrl, dateTaken);
```

### Azure pipeline workflow (new)
```csharp
var photo = new Photo(userId, cameraId, contentHash, thumbBlobName, displayBlobName, 
    takenAtUtc, latitude, longitude);
```

## Migration Command

To apply this migration to your existing database:

```bash
dotnet ef database update --project BuckScience.Infrastructure --startup-project BuckScience.API
```

The migration is designed to work with your existing Photos table structure and data.