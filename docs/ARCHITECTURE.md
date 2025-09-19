# BuckScience Application Architecture

## 🏗️ Overview

BuckScience is built using Clean Architecture principles with a clear separation of concerns, ensuring maintainability, testability, and scalability. The application follows Domain-Driven Design (DDD) patterns and implements CQRS for business operations.

### Architecture Principles
- **Clean Architecture**: Domain-centric design with dependency inversion
- **Domain-Driven Design**: Rich domain models with business logic encapsulation
- **CQRS**: Command/Query separation for optimal read/write operations
- **Dependency Injection**: Comprehensive IoC container usage
- **Single Responsibility**: Each layer has a distinct purpose

---

## 🎯 Solution Structure

### Project Dependencies
```
┌─────────────────────────────────────────┐
│             BuckScience.Web             │ ◄── Presentation Layer
│         (Controllers, Views, UI)        │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│        BuckScience.Infrastructure       │ ◄── Infrastructure Layer
│     (EF Core, External APIs, Auth)      │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│        BuckScience.Application          │ ◄── Application Layer
│       (Use Cases, Business Logic)       │
└─────────────────┬───────────────────────┘
                  │
┌─────────────────▼───────────────────────┐
│          BuckScience.Domain             │ ◄── Domain Layer
│       (Entities, Business Rules)        │
└─────────────────────────────────────────┘
                  ▲
┌─────────────────┴───────────────────────┐
│          BuckScience.Shared             │ ◄── Shared Utilities
│      (Configuration, Constants)         │
└─────────────────────────────────────────┘
```

---

## 🧱 Layer Details

### Domain Layer (Core)
**Purpose**: Contains the core business logic, entities, and domain rules.

#### Key Components
```csharp
// Core Entities
public class Profile        // Individual deer tracking
public class Property       // Hunting property with spatial data
public class PropertyFeature // Spatial features (food plots, water, etc.)
public class Camera         // Trail camera locations
public class Photo          // Trail camera images with metadata
public class Weather        // Weather data for correlation

// Value Objects & Enums
public enum Season { EarlySeason, PreRut, Rut, PostRut, LateSeasonPost, YearRound }
public enum ClassificationType { FoodPlot, WaterSource, CreekCrossing, TravelCorridor, ... }
public enum ProfileStatus { Active, Inactive }

// Domain Services
public static class FeatureHelper     // Feature classification utilities
public static class FeatureWeightHelper // Weight calculation logic
```

#### Business Rules
- **Profile-Property Association**: Profiles must belong to a property owned by the user
- **Camera Placement History**: Cameras can be moved; historical locations are maintained
- **Feature Weight Constraints**: Weights must be between 0.0 and 1.0
- **Spatial Data Integrity**: All geographic data uses WGS84 (SRID 4326)

### Application Layer (Use Cases)
**Purpose**: Orchestrates business workflows and implements use cases.

#### CQRS Pattern Implementation
```csharp
// Command Operations (Write)
public static class CreateProfile
{
    public static async Task<int> HandleAsync(string name, int propertyId, int tagId, ProfileStatus status, ...);
}

public static class UpdateProfile  
{
    public static async Task HandleAsync(int profileId, string newName, ProfileStatus status, ...);
}

// Query Operations (Read)
public static class GetProfile
{
    public record Result(int Id, string Name, ProfileStatus Status, ...);
    public static async Task<Result?> HandleAsync(int profileId, IAppDbContext db, int userId, ...);
}

public static class ListPropertyProfiles
{
    public static async Task<List<ProfileListItem>> HandleAsync(IAppDbContext db, int userId, int propertyId, ...);
}
```

#### Key Services
```csharp
// Analytics Services
public class BuckLensAnalyticsService    // Profile analytics and charts
public class SeasonMonthMappingService   // Season-to-month mapping logic

// Feature Management
public static class GetFeatureWeights   // Weight resolution with seasonal overrides
public static class UpdateFeatureWeights // Weight modification operations
public static class MaterializeFeatureWeights // Compute effective weights

// Photo Processing
public static class ProcessPhotoUpload  // EXIF extraction and weather assignment
public static class TagPhotosBatch      // Bulk photo tagging operations
```

### Infrastructure Layer
**Purpose**: Implements external dependencies and provides data access.

#### Data Persistence
```csharp
// Entity Framework Configuration
public class AppDbContext : DbContext, IAppDbContext
{
    public DbSet<Profile> Profiles { get; }
    public DbSet<Property> Properties { get; }
    public DbSet<PropertyFeature> PropertyFeatures { get; }
    // ... other entities
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Spatial data configuration
        modelBuilder.Entity<Property>()
            .Property(p => p.Center)
            .HasColumnType("geometry")
            .HasSrid(4326);
    }
}

// Configuration Classes
public class PropertyConfiguration : IEntityTypeConfiguration<Property>
public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
// ... other configurations
```

#### External Integrations
```csharp
// Weather Service
public interface IWeatherService
{
    Task<WeatherData> FetchDayWeatherDataAsync(double latitude, double longitude, DateOnly date);
    Task<Weather?> FindWeatherRecordAsync(double latitude, double longitude, DateOnly date, int hour);
}

public class WeatherService : IWeatherService
{
    // VisualCrossing API integration with batch processing optimization
}

// Blob Storage Service
public interface IBlobStorageService
{
    Task<string> UploadPhotoAsync(Stream photoStream, string fileName, string containerName);
    Task<bool> DeletePhotoAsync(string blobPath);
}

// Authentication Service
public interface ICurrentUserService
{
    int? Id { get; }
    string? Email { get; }
    ClaimsPrincipal? User { get; }
}
```

#### Service Registration
```csharp
// DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(conn, sql => sql.UseNetTopologySuite()));
        
        // External APIs
        services.AddHttpClient<WeatherService>();
        services.AddScoped<IWeatherService, WeatherService>();
        
        // Storage
        services.AddSingleton<IBlobStorageService>(provider => 
            new BlobStorageService(connectionString, logger));
        
        // Geometry Factory for spatial operations
        services.AddSingleton<GeometryFactory>(_ =>
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));
        
        return services;
    }
}
```

### Presentation Layer (Web)
**Purpose**: Handles user interface, API endpoints, and user interactions.

#### MVC Controllers
```csharp
[Authorize]
public class BuckTraxController : Controller
{
    // Main BuckTrax interface
    [HttpGet("/bucktrax")]
    public async Task<IActionResult> Index(CancellationToken ct);
    
    // API endpoints for prediction generation
    [HttpPost("/bucktrax/api/predict")]
    public async Task<IActionResult> PredictMovement([FromBody] BuckTraxPredictionRequest request, CancellationToken ct);
}

[Authorize]
public class ProfilesController : Controller
{
    // Profile management and analytics
    [HttpGet("/profiles/{id}")]
    public async Task<IActionResult> Details(int id, CancellationToken ct);
    
    // BuckLens analytics endpoints
    [HttpGet("/profiles/{id}/analytics/charts/cameras")]
    public async Task<IActionResult> GetCameraChart(int id, CancellationToken ct);
}
```

#### ViewModels
```csharp
// BuckTrax ViewModels
public class BuckTraxPredictionRequest  // Input for movement prediction
public class BuckTraxPredictionResult   // Complete prediction results
public class BuckTraxMovementCorridor   // Individual corridor data
public class BuckTraxTimeSegmentPrediction // Time-based predictions

// Analytics ViewModels  
public class ProfileAnalyticsVm         // Profile analytics dashboard
public class ChartData                  // Generic chart data structure
```

---

## 🔄 Data Flow Architecture

### BuckTrax Prediction Flow
```
User Request → Controller Validation → Profile Ownership Check → 
Sighting Collection → Feature Association → Movement Analysis → 
Corridor Scoring → Time Segmentation → Visualization Data → Response
```

#### Detailed Flow
1. **Request Validation**
   ```csharp
   // Validate user authentication
   if (_currentUser.Id is null) return Forbid();
   
   // Validate profile ownership through property association
   var profile = await GetProfile.HandleAsync(profileId, _db, _currentUser.Id.Value, ct);
   if (profile == null) return NotFound();
   ```

2. **Data Collection**
   ```csharp
   // Complex query with historical accuracy
   var sightings = await GetProfileSightings(profileId, profile.TagId, profile.PropertyId, ct);
   
   // Group photos into meaningful sightings (15-minute windows)
   var groupedSightings = GroupPhotosIntoSightings(sightings);
   
   // Load property features with seasonal weights
   var features = await GetPropertyFeaturesWithWeights(profile.PropertyId, season, ct);
   ```

3. **Movement Analysis**
   ```csharp
   // Associate sightings with nearest features
   var sightingsWithFeatures = AssociateSightingsWithFeatures(groupedSightings, features, config.CameraFeatureProximityMeters);
   
   // Identify movement routes with feature-aware pathfinding
   var routes = IdentifyMovementRoutes(sightingsWithFeatures, features, config);
   
   // Calculate corridor scores using feature weights
   var corridors = AnalyzeMovementCorridors(routes, features, config);
   ```

4. **Time Segmentation**
   ```csharp
   // Dynamic time periods based on property daylight hours
   var dayStart = profile.Property.DayHour;
   var dayEnd = profile.Property.NightHour;
   var segmentDuration = CalculateDaylightSpan(dayStart, dayEnd) / 3;
   
   // Generate time-specific predictions
   var timeSegments = GenerateTimeSegmentPredictions(corridors, sightings, timeConfiguration);
   ```

### Photo Upload Flow
```
File Upload → EXIF Extraction → GPS Validation → Weather Lookup → 
Batch Processing → Camera Association → Storage → Database Record
```

#### Batch Processing Optimization
```csharp
// Group photos by location and date for efficient weather API calls
var photoGroups = photos.GroupBy(p => new { 
    Latitude = Round(p.Latitude, 2),    // ~1km precision
    Longitude = Round(p.Longitude, 2),
    Date = p.DateTaken.Date
});

// Single weather API call per group instead of per photo
foreach (var group in photoGroups)
{
    var weatherData = await _weatherService.FetchDayWeatherDataAsync(
        group.Key.Latitude, group.Key.Longitude, DateOnly.FromDateTime(group.Key.Date));
    
    // Assign weather to all photos in group
    foreach (var photo in group)
    {
        photo.WeatherId = FindClosestWeatherRecord(weatherData, photo.DateTaken).Id;
    }
}
```

---

## 🗃️ Database Design

### Entity Relationships
```
Properties (1) ──── (n) Cameras
    │                   │
    │                   │
    │               (1) │ (n)
    │                Photos ──── (n) PhotoTags ──── (n) Tags
    │                   │                               │
    │               (1) │                               │ (1)
    │                Weather                         Profiles
    │                                                   │
    │                                               (1) │ (n)
    │                                              PropertyTags
    │
    └── (n) PropertyFeatures
    │
    └── (n) FeatureWeights
    │
    └── (n) CameraPlacementHistory
```

### Spatial Data Design
```sql
-- Properties with center point and optional boundary
CREATE TABLE Properties (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(255) NOT NULL,
    Center geometry NOT NULL,           -- SRID 4326 (WGS84)
    Boundary geometry NULL,             -- Optional property boundary
    ApplicationUserId int NOT NULL
);

-- Features with various geometry types
CREATE TABLE PropertyFeatures (
    Id int IDENTITY(1,1) PRIMARY KEY,
    PropertyId int NOT NULL,
    ClassificationType int NOT NULL,
    Geometry geometry NOT NULL,         -- Point, LineString, or Polygon
    Name nvarchar(255) NULL,
    Weight float NULL                   -- 0.0 to 1.0
);

-- Spatial indexes for performance
CREATE SPATIAL INDEX SI_Properties_Center ON Properties(Center);
CREATE SPATIAL INDEX SI_PropertyFeatures_Geometry ON PropertyFeatures(Geometry);
```

### Performance Optimization
```sql
-- Composite indexes for common query patterns
CREATE INDEX IX_Photos_CameraId_DateTaken ON Photos(CameraId, DateTaken);
CREATE INDEX IX_PhotoTags_TagId_PhotoId ON PhotoTags(TagId, PhotoId);
CREATE INDEX IX_CameraPlacementHistory_CameraId_DateRange 
    ON CameraPlacementHistories(CameraId, StartDate, EndDate);

-- Weather lookup optimization
CREATE INDEX IX_Weather_Location_Date_Hour 
    ON Weathers(Latitude, Longitude, Date, Hour);
```

---

## 🔧 Configuration Architecture

### Settings Hierarchy
```csharp
// Base configuration
public class BuckTraxConfiguration
{
    public int MovementTimeWindowMinutes { get; set; } = 480;      // 8 hours
    public int MaxMovementDistanceMeters { get; set; } = 5000;     // 5 km  
    public int CameraFeatureProximityMeters { get; set; } = 100;   // 100 meters
    public int MinimumSightingsThreshold { get; set; } = 10;
    public int MinimumTransitionsThreshold { get; set; } = 3;
    public bool ShowLimitedDataWarning { get; set; } = true;
    
    // Feature-aware routing
    public bool EnableFeatureAwareRouting { get; set; } = true;
    public int MinimumDistanceForFeatureRouting { get; set; } = 200;
    public double MaximumDetourPercentage { get; set; } = 0.3;
    public int MaximumWaypointsPerRoute { get; set; } = 2;
}

// Environment-specific overrides
public class WeatherApiSettings
{
    public const string SectionName = "WeatherAPISettings";
    public string BaseUrl { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
}
```

### Configuration Binding
```csharp
// Program.cs
builder.Services.Configure<WeatherApiSettings>(
    builder.Configuration.GetSection(WeatherApiSettings.SectionName));

// Usage in services
public class WeatherService
{
    private readonly WeatherApiSettings _settings;
    
    public WeatherService(IOptions<WeatherApiSettings> settings)
    {
        _settings = settings.Value;
    }
}
```

---

## 🔒 Security Architecture

### Authentication Flow
```
User Request → Azure AD B2C → JWT Token → Cookie Authentication → 
Controller Authorization → User Context → Data Access Control
```

#### Implementation
```csharp
// Authentication configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAdB2C"));

// Authorization policies
[Authorize]
public class BuckTraxController : Controller
{
    // All endpoints require authentication
    
    private async Task<IActionResult> ValidateProfileOwnership(int profileId)
    {
        var profile = await _db.Profiles
            .Join(_db.Properties, p => p.PropertyId, prop => prop.Id, (p, prop) => new { Profile = p, Property = prop })
            .Where(x => x.Profile.Id == profileId && x.Property.ApplicationUserId == _currentUser.Id.Value)
            .FirstOrDefaultAsync();
            
        return profile == null ? Forbid() : Ok();
    }
}
```

### Data Access Security
```csharp
// User context service
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public int? Id => GetUserIdFromClaims();
    public ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;
    
    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User?.FindFirst("sub") ?? User?.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }
}

// Data scoping in all queries
public static async Task<List<ProfileListItem>> HandleAsync(IAppDbContext db, int userId, int propertyId, CancellationToken ct)
{
    return await db.Profiles
        .Join(db.Properties, p => p.PropertyId, prop => prop.Id, (p, prop) => new { Profile = p, Property = prop })
        .Where(x => x.Property.ApplicationUserId == userId && x.Property.Id == propertyId) // Always filter by user
        .Select(x => new ProfileListItem(...))
        .ToListAsync(ct);
}
```

---

## 📊 Monitoring & Observability

### Logging Architecture
```csharp
// Structured logging throughout the application
public class BuckTraxController : Controller
{
    private readonly ILogger<BuckTraxController> _logger;
    
    public async Task<IActionResult> PredictMovement([FromBody] BuckTraxPredictionRequest request, CancellationToken ct)
    {
        _logger.LogInformation("Starting movement prediction for profile {ProfileId} by user {UserId}", 
            request.ProfileId, _currentUser.Id);
        
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var result = await GenerateMovementPredictions(request.ProfileId, request.Season, ct);
            stopwatch.Stop();
            
            _logger.LogInformation("Prediction completed in {ElapsedMs}ms for {SightingCount} sightings, generated {CorridorCount} corridors",
                stopwatch.ElapsedMilliseconds, result.TotalSightings, result.MovementCorridors.Count);
                
            return Json(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating movement prediction for profile {ProfileId}", request.ProfileId);
            return BadRequest($"Error generating predictions: {ex.Message}");
        }
    }
}
```

### Performance Monitoring
```csharp
// Custom metrics for algorithm performance
public class PerformanceMetrics
{
    public static readonly Counter PredictionRequests = Metrics
        .CreateCounter("bucktrax_prediction_requests_total", "Total prediction requests");
        
    public static readonly Histogram PredictionDuration = Metrics
        .CreateHistogram("bucktrax_prediction_duration_seconds", "Prediction generation time");
        
    public static readonly Gauge ActivePredictions = Metrics
        .CreateGauge("bucktrax_active_predictions", "Currently processing predictions");
}

// Usage in controller
using (PredictionDuration.NewTimer())
{
    PredictionRequests.Inc();
    ActivePredictions.Inc();
    
    try
    {
        var result = await GenerateMovementPredictions(...);
        return Json(result);
    }
    finally
    {
        ActivePredictions.Dec();
    }
}
```

---

## 🚀 Scalability Considerations

### Current Architecture Benefits
- **Stateless Controllers**: Support horizontal scaling
- **Database Connection Pooling**: Efficient connection management
- **Async/Await Pattern**: Non-blocking I/O operations
- **Query Optimization**: Efficient database access patterns

### Future Scaling Options
```csharp
// Background processing for heavy operations
public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
    ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);
}

// Caching layer for expensive operations
public class CachedFeatureWeightService
{
    private readonly IMemoryCache _cache;
    private readonly IFeatureWeightService _featureWeightService;
    
    public async Task<List<FeatureWeight>> GetFeatureWeightsAsync(int propertyId, Season season)
    {
        var cacheKey = $"feature-weights-{propertyId}-{season}";
        
        if (!_cache.TryGetValue(cacheKey, out List<FeatureWeight> weights))
        {
            weights = await _featureWeightService.GetFeatureWeightsAsync(propertyId, season);
            _cache.Set(cacheKey, weights, TimeSpan.FromMinutes(30));
        }
        
        return weights;
    }
}

// Database read replicas for analytics
public class AnalyticsDbContext : DbContext
{
    // Read-only context for analytics queries
    // Could point to read replica for better performance
}
```

---

## 🧪 Testing Architecture

### Test Pyramid Implementation
```
                    ┌─────────────────────┐
                    │   Integration Tests │  ← Full application flow
                    │    (Controllers)    │
                    └─────────────────────┘
               ┌─────────────────────────────────┐
               │     Application Tests           │  ← Business logic
               │  (Use Cases, Services)          │
               └─────────────────────────────────┘
          ┌───────────────────────────────────────────┐
          │              Unit Tests                   │  ← Individual components
          │    (Entities, Algorithms, Helpers)       │
          └───────────────────────────────────────────┘
```

#### Test Examples
```csharp
// Unit Tests - Domain Logic
[Fact]
public void Profile_Rename_ShouldUpdateName()
{
    var profile = new Profile("Old Name", 1, 1, ProfileStatus.Active);
    profile.Rename("New Name");
    Assert.Equal("New Name", profile.Name);
}

// Application Tests - Use Cases
[Fact]
public async Task GetProfile_WithValidId_ShouldReturnProfile()
{
    var result = await GetProfile.HandleAsync(1, _db, userId: 1, CancellationToken.None);
    Assert.NotNull(result);
    Assert.Equal("Test Profile", result.Name);
}

// Integration Tests - Full Flow
[Fact]
public async Task BuckTrax_PredictMovement_ShouldReturnPredictions()
{
    var request = new BuckTraxPredictionRequest { ProfileId = 1 };
    var response = await _client.PostAsJsonAsync("/bucktrax/api/predict", request);
    
    response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<BuckTraxPredictionResult>();
    Assert.NotNull(result);
    Assert.True(result.MovementCorridors.Count > 0);
}
```

---

## 📚 Documentation Strategy

### Multi-Level Documentation
1. **README.md** - High-level overview and getting started
2. **ARCHITECTURE.md** - This document - comprehensive architecture overview
3. **DEVELOPER_GUIDE.md** - Detailed development instructions and patterns
4. **API_DOCUMENTATION.md** - Complete API reference
5. **BUCKTRAX_DEEP_DIVE.md** - Detailed BuckTrax system documentation
6. **Component-Specific Docs** - Individual feature documentation

### Code Documentation
```csharp
/// <summary>
/// Generates movement predictions for a specific deer profile using historical sightings
/// and property features. Implements feature-aware routing and time segmentation.
/// </summary>
/// <param name="profileId">The unique identifier of the deer profile</param>
/// <param name="season">Optional seasonal filter for feature weight adjustment</param>
/// <param name="ct">Cancellation token for async operation control</param>
/// <returns>Complete prediction results including corridors, zones, and time segments</returns>
/// <exception cref="UnauthorizedAccessException">Thrown when user doesn't own the profile</exception>
/// <exception cref="ArgumentException">Thrown when profileId is invalid</exception>
public async Task<BuckTraxPredictionResult> GenerateMovementPredictions(
    int profileId, 
    Season? season, 
    CancellationToken ct)
{
    // Implementation with detailed inline comments
}
```

---

*This architecture documentation provides a comprehensive overview of the BuckScience application structure, design decisions, and implementation patterns. It serves as the foundation for understanding how all components work together to deliver hunting analytics capabilities.*