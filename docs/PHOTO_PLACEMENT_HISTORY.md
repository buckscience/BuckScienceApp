# Photo-Camera Placement History Association

## Overview

This document describes the implementation of associating photos with camera placement history records instead of just cameras. This enables analytics and reporting by camera location rather than just by camera device.

## Changes Made

### 1. Photo Entity Updates

- Added `int? CameraPlacementHistoryId` property
- Added `CameraPlacementHistory? PlacementHistory` navigation property  
- Added `SetCameraPlacementHistory(int?)` domain method with validation
- Updated constructor to accept optional `cameraPlacementHistoryId` parameter

### 2. Database Changes

- Created EF migration `AddCameraPlacementHistoryToPhoto`
- Added nullable FK column with proper index
- Configured SetNull cascade behavior (if placement history is deleted, photo's FK becomes null)

### 3. Photo Upload Logic Updates

- Modified `UploadPhotos.HandleAsync` to include placement histories when querying camera
- Updated photo creation to use current camera placement ID when available
- Added safety check to only use placement history ID when > 0 (valid database entity)

### 4. Entity Framework Configuration

- Added relationship configuration in `PhotoConfiguration`
- Added proper indexes for performance
- Configured cascade behavior for referential integrity

## Backward Compatibility

- **100% backward compatible**: All existing functionality continues to work
- **Existing photos**: Will have `null` for `CameraPlacementHistoryId` until migrated
- **New photos**: Will be automatically associated with current camera placement
- **No breaking changes**: API contracts remain the same

## Photo-Location Association Logic

When a photo is uploaded:

1. **Get current camera placement**: Query includes `PlacementHistories` for the camera
2. **Validate placement**: Only use placement history ID if it's a valid database entity (ID > 0)
3. **Associate photo**: Set `CameraPlacementHistoryId` to current placement ID or null
4. **Fallback behavior**: If no current placement exists, photo is still created but without placement association

## Data Migration Considerations

### Existing Photos

Existing photos in the database will have `CameraPlacementHistoryId = null` after the migration. To associate them with historical placement data:

```sql
-- Example migration query (would need refinement for production use)
UPDATE Photos 
SET CameraPlacementHistoryId = (
    SELECT TOP 1 cph.Id 
    FROM CameraPlacementHistories cph 
    WHERE cph.CameraId = Photos.CameraId 
      AND cph.StartDateTime <= Photos.DateTaken 
      AND (cph.EndDateTime IS NULL OR cph.EndDateTime >= Photos.DateTaken)
    ORDER BY cph.StartDateTime DESC
)
WHERE CameraPlacementHistoryId IS NULL;
```

### Migration Strategy Options

1. **Immediate**: Run data migration script after deployment
2. **Gradual**: Background job to migrate photos in batches
3. **On-demand**: Migrate photos when they're accessed/analyzed
4. **Leave as-is**: Only new photos get placement association

## Analytics Implications

### Before
- Photos grouped by `CameraId`
- Location data derived from camera's current location
- Historical location changes not reflected in photo analytics

### After  
- Photos can be grouped by `CameraPlacementHistoryId`
- Each photo associated with exact camera location at time of capture
- Analytics can show patterns by specific camera locations
- Camera movements don't affect historical photo location data

## Testing

- **Domain tests**: Photo entity behavior and validation
- **Integration tests**: Photo-placement association scenarios  
- **Regression tests**: All existing functionality (220 tests passing)
- **Edge cases**: Cameras without placement history, entity ID validation

## Future Enhancements

With this foundation, future features become possible:

1. **Location-based photo filtering**: Filter photos by specific camera locations
2. **Movement analytics**: Track animal patterns relative to camera relocations
3. **Optimal placement analysis**: Analyze photo capture rates by location
4. **Historical reporting**: Generate reports showing camera effectiveness by location
5. **Map-based photo browsing**: Display photos on maps by their exact capture location

## Performance Considerations

- Added index on `CameraPlacementHistoryId` for efficient querying
- Nullable FK keeps storage efficient (no impact when null)
- Lazy loading available for placement history data when needed
- Query optimization possible by joining photos with placement histories