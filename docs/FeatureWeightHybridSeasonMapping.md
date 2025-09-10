# Feature Weight Hybrid Season Mapping Implementation

This document describes the refactored feature weight retrieval logic that integrates hybrid season-month mapping to determine active seasons from dates.

## Overview

The refactored feature weight retrieval system now supports:

1. **Date-to-Season Resolution**: Given a property and date, the system determines the active season using hybrid season-month mapping
2. **Property-Specific Overrides**: Custom season definitions stored in the database override default enum mappings
3. **Edge Case Handling**: Graceful handling of overlapping seasons, missing overrides, and conflicting configurations
4. **Multiple User Type Support**: Optimized for hunters, land managers, and researchers with different configuration needs

## Architecture Changes

### New Methods in SeasonMonthMappingService

- `GetActiveSeasonsForDateAsync(DateTime date, Property property)`: Returns all seasons active for a given date
- `GetPrimarySeasonForDateAsync(DateTime date, Property property)`: Returns the primary season (first by enum order) for a given date

### Enhanced GetFeatureWeights Handler

- **New Overload**: `HandleAsync(IAppDbContext, SeasonMonthMappingService, int propertyId, DateTime date)` 
- **Hybrid Resolution**: Automatically resolves the active season from the date using property-specific overrides
- **Backward Compatibility**: Original overload `HandleAsync(IAppDbContext, int propertyId, Season? currentSeason)` remains unchanged

## Hybrid Season Resolution Logic

The system follows this prioritized approach:

1. **Check Property Overrides**: Query `PropertySeasonMonthsOverride` table for custom season-month mappings
2. **Validate Override Data**: Ensure override months are valid and not corrupted
3. **Fallback to Defaults**: Use `MonthsAttribute` on Season enum if no valid override exists
4. **Handle Overlaps**: When multiple seasons match a date, return all matches ordered by enum value

## Edge Case Handling

### Overlapping Seasons
- **Example**: October matches both `EarlySeason` (9,10) and `PreRut` (10)
- **Resolution**: `GetActiveSeasonsForDateAsync()` returns both seasons, `GetPrimarySeasonForDateAsync()` returns the first by enum order

### No Matching Seasons
- **Scenario**: All seasons have been overridden to exclude a particular month
- **Resolution**: Methods return empty list or null respectively, feature weights fall back to user/default weights

### Year-Round Season
- **Behavior**: `YearRound` season (includes all 12 months) will always match
- **Impact**: Provides fallback behavior for any date that doesn't match specific seasons

### Invalid Override Data
- **Detection**: JSON deserialization failures or null/empty month arrays
- **Recovery**: System automatically falls back to default season mappings

## User Type Scenarios

### Hunter (Default Mappings)
```csharp
// Uses standard season definitions without customization
var results = await GetFeatureWeights.HandleAsync(db, seasonService, propertyId, new DateTime(2024, 11, 15));
// November resolves to Rut season using default mapping
```

### Land Manager (Selective Overrides)
```csharp
// Custom early season extension for harvest management
await seasonService.SetPropertySeasonOverrideAsync(propertyId, Season.EarlySeason, new[] { 8, 9, 10, 11 });
var results = await GetFeatureWeights.HandleAsync(db, seasonService, propertyId, new DateTime(2024, 8, 15));
// August now resolves to extended EarlySeason
```

### Researcher (Extensive Customization)
```csharp
// Custom rut study period
await seasonService.SetPropertySeasonOverrideAsync(propertyId, Season.Rut, new[] { 10, 11, 12 });
await seasonService.SetPropertySeasonOverrideAsync(propertyId, Season.PostRut, new[] { 1, 2 });
var results = await GetFeatureWeights.HandleAsync(db, seasonService, propertyId, new DateTime(2024, 12, 15));
// December resolves to extended Rut season for research
```

## Testing Coverage

### Unit Tests
- **Date Resolution**: 10 tests covering date-to-season mapping with various scenarios
- **Season Mapping Service**: 30 tests covering hybrid logic, overrides, and edge cases
- **Feature Weight Integration**: Tests for all user types and seasonal scenarios

### Test Categories
1. **Default Mapping Tests**: Verify correct season resolution without overrides
2. **Override Tests**: Test property-specific season customizations
3. **Edge Case Tests**: Handle overlapping seasons, missing data, corrupted overrides
4. **Multi-User Tests**: Simulate hunter, land manager, and researcher workflows
5. **Integration Tests**: End-to-end feature weight retrieval with date resolution

## Performance Considerations

- **Database Queries**: Override lookup is optimized with property and season indexing
- **Caching**: Season resolution results could be cached if performance becomes a concern
- **Enumeration**: All seasons are evaluated for each date, but this is a small, fixed set

## Future Enhancements

1. **Caching Layer**: Add season resolution caching for frequently queried property/date combinations
2. **Validation Service**: Enhanced override validation with conflict detection
3. **Migration Tools**: Utilities to bulk-update season overrides across properties
4. **Analytics**: Track override usage patterns to inform default season definitions

## API Examples

### Basic Date Resolution
```csharp
// Get feature weights for a specific date
var results = await GetFeatureWeights.HandleAsync(
    dbContext, 
    seasonMappingService, 
    propertyId: 123, 
    date: new DateTime(2024, 11, 15)
);
```

### Multiple Season Detection
```csharp
// Find all active seasons for a date
var activeSeasons = await seasonMappingService.GetActiveSeasonsForDateAsync(
    date: new DateTime(2024, 10, 15), 
    property: property
);
// Returns: [EarlySeason, PreRut, YearRound]
```

### Primary Season Selection
```csharp
// Get the primary season for effective weight calculation
var primarySeason = await seasonMappingService.GetPrimarySeasonForDateAsync(
    date: new DateTime(2024, 10, 15), 
    property: property
);
// Returns: EarlySeason (first by enum order)
```