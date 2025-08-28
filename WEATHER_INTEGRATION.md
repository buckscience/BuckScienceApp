# Weather Data Integration Implementation

## Overview
This implementation adds weather data integration to the BuckScience photo upload process with **batch processing optimization**. When photos are uploaded, the system:

1. Extracts GPS coordinates from EXIF data (if available)
2. Falls back to camera location if GPS data is not available
3. **Groups photos by rounded location and date for batch processing**
4. Rounds coordinates to configurable precision (~1km by default)
5. Looks up existing weather data for each location/date group
6. Fetches missing weather data from VisualCrossing API (one call per group)
7. Associates the correct weather record with each photo

## Batch Processing Optimization

### Efficiency Benefits
The upload process now uses **intelligent batch processing** to minimize API calls:

- **Before**: Each photo could trigger a separate weather API call
- **After**: Photos are grouped by location and date, with one API call per unique location/date combination

### Example Scenario
Uploading 100 photos from a trail camera:
- **Old approach**: Up to 100 API calls (if no weather data cached)
- **New approach**: 1-7 API calls (one per unique date, since all photos share same location)

### Grouping Logic
```csharp
// Photos are grouped by rounded location and date
var photoGroups = photos.GroupBy(p => new { 
    Latitude = Round(p.Latitude, 2),    // 40.123456 → 40.12
    Longitude = Round(p.Longitude, 2),  // -74.987654 → -74.99
    Date = p.DateTaken.Date             // 2024-10-15
});
```

## Configuration

### WeatherSettings (appsettings.json)
```json
"WeatherSettings": {
  "LocationRoundingPrecision": 2,    // Decimal places for lat/lng rounding (default: 2 = ~1km)
  "EnableGPSExtraction": true,       // Extract GPS from EXIF data
  "FallbackToCameraLocation": true   // Use camera location if GPS not available
}
```

### WeatherAPISettings (appsettings.json)
```json
"WeatherAPISettings": {
  "BaseUrl": "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/",
  "APIKey": "YOUR_API_KEY"
}
```

## Database Changes

### Weather Table Schema Updates
- Added `Latitude` (float) - Rounded latitude for weather lookup
- Added `Longitude` (float) - Rounded longitude for weather lookup  
- Added `Date` (date) - Date for weather lookup
- Added `Hour` (int) - Hour (0-23) for specific weather record
- Added composite indexes for efficient lookups

### Migration
- Migration `20250828132655_AddLocationToWeather` adds the new fields
- Includes proper indexes for performance

## Key Components

### IWeatherService Interface
Provides methods for:
- `FetchDayWeatherDataAsync()` - Fetches 24-hour weather data from API
- `FindWeatherRecordAsync()` - Finds existing weather record by location/date/hour
- `RoundCoordinates()` - Rounds coordinates to specified precision
- `HasWeatherDataForLocationAndDateAsync()` - Checks if weather data exists for batch processing
- `GetWeatherDataForLocationAndDateAsync()` - Retrieves all hourly weather data for a location/date

### WeatherService Implementation
- Integrates with VisualCrossing API
- Handles weather data caching in database
- Provides coordinate rounding utilities
- **Supports batch processing with optimized database queries**
- Includes comprehensive error handling and logging

### Updated Photo Upload Process
- Enhanced EXIF extraction to include GPS coordinates
- **Two-phase processing: upload files first, then batch process weather data**
- **Intelligent grouping by location and date to minimize API calls**
- Weather lookup and assignment with fallback handling
- Graceful degradation if weather data unavailable

## API Integration

### VisualCrossing API
- Fetches full 24-hour weather data for a location/date
- Returns hourly weather records with comprehensive meteorological data
- Uses metric units
- Stores all hourly records to minimize API calls

## Performance Considerations

### Database Indexes
- Composite index on `(Latitude, Longitude, Date)` for daily lookups
- Composite index on `(Latitude, Longitude, Date, Hour)` for hourly lookups
- Existing datetime indexes maintained

### Caching Strategy
- Weather data stored in database after first fetch
- Subsequent requests for same location/date use cached data
- Rounded coordinates reduce cache misses for nearby locations

### API Efficiency
- Single API call fetches entire day (24 hours) of data
- Reduces API usage compared to hourly requests
- Graceful handling of API failures

## Error Handling

### Photo Upload
- Weather assignment failures don't prevent photo upload
- Logging for troubleshooting weather integration issues
- Photos can be processed without weather data

### API Integration
- Comprehensive error handling for network issues
- Logging of API requests and responses (with masked API keys)
- Fallback behavior when weather service unavailable

## Testing

### Unit Tests
- WeatherService coordinate rounding functionality
- Configuration binding verification
- Service dependency injection validation

### Integration Points
- Photo upload controller updated with weather dependencies
- Dependency injection configured for weather services
- All existing tests continue to pass

## Usage Examples

### Coordinate Rounding
```csharp
var weatherService = serviceProvider.GetService<IWeatherService>();
var (lat, lng) = weatherService.RoundCoordinates(40.123456, -74.987654, 2);
// Result: (40.12, -74.99) - approximately 1km precision
```

### Weather Lookup
```csharp
var weather = await weatherService.FindWeatherRecordAsync(40.12, -74.99, new DateOnly(2024, 8, 28), 14);
// Finds weather record for 2:00 PM on August 28, 2024
```

## Deployment Notes

### Database Migration
Run the migration to add weather table fields:
```bash
dotnet ef database update --context AppDbContext
```

### Configuration
Ensure WeatherSettings and WeatherAPISettings are properly configured in production appsettings.

### API Key
Secure the VisualCrossing API key using proper secret management in production.

## Future Enhancements

1. **Background Processing**: Move weather fetching to background jobs
2. **Cache Warming**: Pre-fetch weather data for common locations
3. **Weather Analytics**: Add weather-based photo analysis features
4. **Multiple Providers**: Support additional weather API providers
5. **Batch Processing**: Batch weather requests for multiple photos