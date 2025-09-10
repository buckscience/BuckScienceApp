# Hybrid Season-Month Mapping Implementation

This document describes the hybrid season-to-month mapping system implemented in BuckScienceApp.

## Overview

The system provides a flexible approach to map hunting seasons to calendar months, supporting both:
1. **Default mappings** defined via attributes on the Season enum
2. **Property-specific overrides** stored in the database

## Architecture

### Default Mappings

Default season-to-month mappings are defined using the `MonthsAttribute` on Season enum values:

```csharp
public enum Season
{
    [Months(9, 10)]
    EarlySeason = 1,
    
    [Months(10)]
    PreRut = 2,
    
    [Months(11)]
    Rut = 3,
    
    [Months(12)]
    PostRut = 4,
    
    [Months(12, 1)]
    LateSeason = 5,
    
    [Months(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)]
    YearRound = 6
}
```

These defaults can be retrieved using the extension method:
```csharp
var months = Season.PreRut.GetDefaultMonths(); // Returns [10]
```

### Property-Specific Overrides

Custom mappings per property are stored in the `PropertySeasonMonthsOverride` table with the following structure:

| Column | Type | Description |
|--------|------|-------------|
| Id | int | Primary key |
| PropertyId | int | Foreign key to Property table |
| Season | int | Season enum value |
| MonthsJson | nvarchar(100) | JSON array of month integers |
| CreatedAt | datetime2 | Creation timestamp |
| UpdatedAt | datetime2 | Last update timestamp |

### Lookup Logic

The system follows this priority order:

1. **Check for property-specific override** in the database
2. **Fall back to default mapping** from the MonthsAttribute

This is implemented in the `SeasonMonthMappingService` class:

```csharp
public async Task<int[]> GetMonthsForPropertyAsync(Season season, Property property, CancellationToken cancellationToken = default)
{
    if (property == null)
        return season.GetDefaultMonths();

    // Check for property-specific override
    var override_ = await _dbContext.PropertySeasonMonthsOverrides
        .FirstOrDefaultAsync(o => o.PropertyId == property.Id && o.Season == season, cancellationToken);

    if (override_ != null)
    {
        var overrideMonths = override_.GetMonths();
        if (overrideMonths != null && overrideMonths.Length > 0)
        {
            return overrideMonths;
        }
    }

    // Fall back to default months
    return season.GetDefaultMonths();
}
```

## Usage Examples

### Getting Default Months
```csharp
// Get default months for a season
var preRutMonths = Season.PreRut.GetDefaultMonths(); // [10]
var rutMonths = Season.Rut.GetDefaultMonths(); // [11]
```

### Using the Service (Recommended)
```csharp
// Inject the service
public class HuntingController : Controller
{
    private readonly SeasonMonthMappingService _seasonService;
    
    public HuntingController(SeasonMonthMappingService seasonService)
    {
        _seasonService = seasonService;
    }
    
    public async Task<IActionResult> GetSeasonMonths(int propertyId, Season season)
    {
        var property = await GetPropertyById(propertyId);
        var months = await _seasonService.GetMonthsForPropertyAsync(season, property);
        return Json(months);
    }
}
```

### Managing Overrides
```csharp
// Set a custom override
await _seasonService.SetPropertySeasonOverrideAsync(propertyId: 1, Season.PreRut, new[] { 9, 10 });

// Remove an override (falls back to default)
await _seasonService.RemovePropertySeasonOverrideAsync(propertyId: 1, Season.PreRut);

// Get all overrides for a property
var overrides = await _seasonService.GetAllPropertyOverridesAsync(propertyId: 1);
```

## Database Migration

To use this feature, you'll need to create and run a migration:

```bash
dotnet ef migrations add AddPropertySeasonMonthsOverride
dotnet ef database update
```

## Testing

The implementation includes comprehensive unit tests covering:

- **MonthsAttribute**: Validation and constructor behavior
- **SeasonExtensions**: Default month retrieval via reflection
- **PropertySeasonMonthsOverride**: Entity behavior and JSON serialization
- **SeasonMonthMappingService**: Basic service functionality

## Configuration

The system requires no additional configuration beyond:

1. Adding the `PropertySeasonMonthsOverride` entity to your DbContext (already done)
2. Registering the `SeasonMonthMappingService` in your DI container
3. Running the database migration

## Benefits

1. **Flexibility**: Property owners can customize season definitions for their specific location
2. **Defaults**: Sensible defaults work out of the box for most users
3. **Performance**: Attribute-based defaults avoid database lookups when no overrides exist
4. **Maintainability**: Clear separation between default and override logic
5. **Type Safety**: Compile-time checking of default mappings via attributes