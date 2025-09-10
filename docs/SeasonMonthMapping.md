# Hybrid Season-Month Mapping Implementation

This document describes the hybrid season-to-month mapping system implemented in BuckScienceApp, which provides flexible hunting season definitions that support both typical hunting scenarios and advanced research/land management use cases.

## Overview

The system provides a two-tier approach to map hunting seasons to calendar months:

1. **Default mappings** - Sensible defaults defined via `MonthsAttribute` on Season enum values
2. **Property-specific overrides** - Custom mappings stored in the database for specific properties

### Why Hybrid Mapping?

**Typical Hunting Scenarios**: Most hunters benefit from standardized season definitions that reflect common hunting patterns across regions (e.g., rut season in November, late season in December-January).

**Advanced Research/Land Management**: Property managers and researchers may need custom season definitions based on:
- Regional climate variations (northern vs. southern properties)
- Local deer behavior patterns observed through trail cameras
- Specific management goals (extending seasons for harvest management)
- Research studies requiring non-standard time periods

The hybrid approach ensures **ease of use** for typical scenarios while providing **full customization** for advanced use cases.

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

## Hybrid Lookup Logic

The system implements a **priority-based lookup** that follows this decision tree:

```
1. Property-specific override exists?
   ├─ YES: Use override months
   │  ├─ Override has valid data?
   │  │  ├─ YES: Return override months ✓
   │  │  └─ NO: Fall back to defaults (step 2)
   │  └─ Override is empty/null: Fall back to defaults (step 2)
   └─ NO: Use default months from MonthsAttribute ✓

2. Default months from Season enum MonthsAttribute
```

### Implementation Details

This hybrid logic is implemented in the `SeasonMonthMappingService` class:

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
            return overrideMonths;  // Use custom override
        }
    }

    // Fall back to default months
    return season.GetDefaultMonths();  // Use enum defaults
}
```

### API Usage Patterns

**For Default Months Only** (No Database Access):
```csharp
// Extension method - fast, no DB dependency
var months = Season.PreRut.GetDefaultMonths(); // Returns [10]
```

**For Hybrid Logic** (Database Access Required):
```csharp
// Service method - checks overrides then defaults
var months = await _seasonService.GetMonthsForPropertyAsync(Season.PreRut, property);
// Returns [9, 10] if override exists, otherwise [10] (default)
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

- **MonthsAttribute**: Validation and constructor behavior (6 tests)
- **SeasonExtensions**: Default month retrieval via reflection (8 tests)  
- **PropertySeasonMonthsOverride**: Entity behavior and JSON serialization (16 tests)
- **SeasonMonthMappingService**: Hybrid logic scenarios including defaults, overrides, and edge cases (18 tests)

### Test Coverage Areas

**Default Scenario Testing**:
- All season enum values return correct default months
- Extension methods work without database dependencies
- Invalid enum values handled properly

**Override Scenario Testing**:
- Property-specific overrides take precedence over defaults
- Multiple properties can have different overrides for the same season
- Invalid/corrupted overrides fall back to defaults gracefully
- Override creation, updates, and deletion work correctly

**Edge Case Testing**:
- Null properties handled correctly
- Empty override data falls back to defaults
- Concurrent access scenarios
- Database transaction integrity

## Configuration

The system requires no additional configuration beyond:

1. Adding the `PropertySeasonMonthsOverride` entity to your DbContext (already done)
2. Registering the `SeasonMonthMappingService` in your DI container
3. Running the database migration

## Real-World Use Cases

### Typical Hunting Scenarios (Default Mappings)

**Recreational Hunter in Ohio**:
- Uses standard season definitions without any customization
- `Season.Rut.GetDefaultMonths()` returns `[11]` (November)
- Benefits from established hunting wisdom encoded in defaults

**Hunting Club in Pennsylvania**:
- Most seasons work fine with defaults
- Only needs to override `EarlySeason` to extend from `[9, 10]` to `[8, 9, 10]`
- Falls back to defaults for all other seasons

### Advanced Research/Land Management (Override Scenarios)

**University Deer Research Project**:
- Studying rut timing variations across latitude
- Northern property: Rut season `[10, 11]` (earlier than default)
- Southern property: Rut season `[11, 12]` (later than default)
- Each property has custom overrides while maintaining data consistency

**Commercial Hunting Ranch**:
- Extends hunting seasons for revenue optimization
- Early season: `[8, 9, 10, 11]` (longer than default `[9, 10]`)
- Late season: `[12, 1, 2]` (longer than default `[12, 1]`)
- Custom seasons based on deer population management goals

**Wildlife Management Area**:
- Different zones have different season timing
- Zone A (Agricultural): Earlier seasons due to crop harvest patterns
- Zone B (Forest): Standard default seasons work well
- Zone C (Research): Completely custom seasons for monitoring studies

### Data Integration Benefits

The hybrid approach enables:
- **Standardized reporting** across properties using consistent season names
- **Customized analysis** respecting local variations in timing
- **Easy onboarding** for new users (defaults work immediately)
- **Advanced customization** without breaking existing functionality