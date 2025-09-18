# BuckScience Developer Guide

## 🚀 Getting Started

### Prerequisites
- **.NET 8.0 SDK** - Download from [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
- **SQL Server** - Local instance or SQL Server Express with spatial data support
- **Visual Studio 2022** or **VS Code** with C# extensions
- **Azure AD B2C Tenant** - For authentication setup
- **VisualCrossing Weather API Key** - For weather integration
- **Azure Storage Account** - For photo storage

### Initial Setup

1. **Clone Repository**
   ```bash
   git clone https://github.com/buckscience/BuckScienceApp.git
   cd BuckScienceApp
   ```

2. **Restore Dependencies**
   ```bash
   dotnet restore
   ```

3. **Database Setup**
   ```bash
   # Create database and run migrations
   dotnet ef database update --context AppDbContext --project src/BuckScience.Infrastructure --startup-project src/BuckScience.Web
   ```

4. **Configuration**
   ```bash
   # Copy template settings
   cp src/BuckScience.Web/appsettings.Development.template.json src/BuckScience.Web/appsettings.Development.json
   # Edit with your connection strings and API keys
   ```

5. **Build & Run**
   ```bash
   dotnet build
   dotnet run --project src/BuckScience.Web
   ```

---

## 🏗️ Project Structure

### Solution Architecture
```
BuckScience.sln
├── src/
│   ├── BuckScience.Domain/          # Core business entities and rules
│   │   ├── Entities/                # Entity classes (Profile, Property, etc.)
│   │   ├── Enums/                   # Business enumerations
│   │   └── Helpers/                 # Domain helper classes
│   ├── BuckScience.Application/     # Business logic and use cases
│   │   ├── Abstractions/            # Interfaces and contracts
│   │   ├── Analytics/               # BuckLens analytics services
│   │   ├── FeatureWeights/          # Feature weight management
│   │   ├── Photos/                  # Photo processing logic
│   │   ├── Profiles/                # Profile management
│   │   └── Properties/              # Property management
│   ├── BuckScience.Infrastructure/  # External dependencies
│   │   ├── Auth/                    # Authentication services
│   │   ├── Persistence/             # Entity Framework configuration
│   │   └── Services/                # External API integrations
│   ├── BuckScience.Web/            # MVC web application
│   │   ├── Controllers/             # MVC controllers
│   │   ├── Views/                   # Razor views
│   │   ├── ViewModels/              # View model classes
│   │   └── wwwroot/                # Static assets
│   ├── BuckScience.Shared/         # Shared utilities
│   └── BuckScience.API/            # API-only project (if needed)
├── tests/
│   └── BuckScience.Tests/          # Comprehensive test suite
└── docs/                           # Documentation
```

### Key Design Patterns

#### Clean Architecture
- **Domain Layer**: Core business logic, entities, and rules
- **Application Layer**: Use cases and business workflows
- **Infrastructure Layer**: External dependencies (DB, APIs, etc.)
- **Presentation Layer**: Controllers, views, and user interfaces

#### CQRS (Command Query Responsibility Segregation)
```csharp
// Commands for writing operations
public class CreateProfile
{
    public static async Task<int> HandleAsync(/* parameters */) { }
}

// Queries for reading operations  
public class GetProfile
{
    public static async Task<Result?> HandleAsync(/* parameters */) { }
}
```

#### Repository Pattern
```csharp
// Abstracted through IAppDbContext interface
public interface IAppDbContext
{
    DbSet<Profile> Profiles { get; }
    DbSet<Property> Properties { get; }
    // ... other entities
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

---

## 🗃️ Database Schema

### Core Entities

#### Properties
```sql
CREATE TABLE Properties (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(255) NOT NULL,
    Center geometry NOT NULL,           -- Spatial center point
    Boundary geometry NULL,             -- Property boundary polygon
    TimeZone nvarchar(50) NOT NULL,
    DayHour int NOT NULL,              -- Dawn hour for time calculations
    NightHour int NOT NULL,            -- Dusk hour for time calculations
    ApplicationUserId int NOT NULL,     -- Owner reference
    CreatedDate datetime2 NOT NULL
);
```

#### Profiles (Individual Deer)
```sql
CREATE TABLE Profiles (
    Id int IDENTITY(1,1) PRIMARY KEY,
    Name nvarchar(255) NOT NULL,
    ProfileStatus int NOT NULL,         -- Active/Inactive status
    PropertyId int NOT NULL,            -- Property association
    TagId int NOT NULL,                 -- Tag for photo identification
    CoverPhotoUrl nvarchar(500) NULL,
    FOREIGN KEY (PropertyId) REFERENCES Properties(Id),
    FOREIGN KEY (TagId) REFERENCES Tags(Id)
);
```

#### Photos
```sql
CREATE TABLE Photos (
    Id int IDENTITY(1,1) PRIMARY KEY,
    FilePath nvarchar(500) NOT NULL,
    DateTaken datetime2 NOT NULL,
    CameraId int NOT NULL,
    Latitude float NULL,                -- GPS from EXIF if available
    Longitude float NULL,               -- GPS from EXIF if available
    WeatherId int NULL,                 -- Associated weather data
    FOREIGN KEY (CameraId) REFERENCES Cameras(Id),
    FOREIGN KEY (WeatherId) REFERENCES Weathers(Id)
);
```

#### PropertyFeatures
```sql
CREATE TABLE PropertyFeatures (
    Id int IDENTITY(1,1) PRIMARY KEY,
    PropertyId int NOT NULL,
    ClassificationType int NOT NULL,    -- Feature type enum
    Geometry geometry NOT NULL,         -- Spatial feature geometry
    Name nvarchar(255) NULL,
    Notes nvarchar(max) NULL,
    Weight float NULL,                  -- Individual feature weight (0.0-1.0)
    CreatedAt datetime2 NOT NULL,
    FOREIGN KEY (PropertyId) REFERENCES Properties(Id)
);
```

#### FeatureWeights (Seasonal Weighting System)
```sql
CREATE TABLE FeatureWeights (
    Id int IDENTITY(1,1) PRIMARY KEY,
    PropertyId int NOT NULL,
    ClassificationType int NOT NULL,
    DefaultWeight float NOT NULL,       -- System default weight
    UserWeight float NULL,              -- User-customized weight
    SeasonalWeights nvarchar(max) NULL, -- JSON: seasonal overrides
    FOREIGN KEY (PropertyId) REFERENCES Properties(Id)
);
```

### Spatial Data Configuration

#### NetTopologySuite Integration
```csharp
// In AppDbContext.OnModelCreating()
modelBuilder.Entity<Property>()
    .Property(p => p.Center)
    .HasColumnType("geometry")
    .HasSrid(4326);  // WGS84 coordinate system

modelBuilder.Entity<PropertyFeature>()
    .Property(f => f.Geometry)
    .HasColumnType("geometry")
    .HasSrid(4326);
```

#### Spatial Indexes
```sql
-- Performance indexes for spatial queries
CREATE SPATIAL INDEX SI_Properties_Center ON Properties(Center);
CREATE SPATIAL INDEX SI_PropertyFeatures_Geometry ON PropertyFeatures(Geometry);

-- Composite indexes for common queries
CREATE INDEX IX_Photos_CameraId_DateTaken ON Photos(CameraId, DateTaken);
CREATE INDEX IX_PhotoTags_TagId_PhotoId ON PhotoTags(TagId, PhotoId);
```

---

## 🧪 Testing Strategy

### Test Structure
```
tests/BuckScience.Tests/
├── Application/                    # Application layer tests
│   ├── PropertyFeatureApplicationTests.cs
│   └── FeatureWeightTests.cs
├── Domain/                        # Domain entity tests
│   ├── ProfileTests.cs
│   └── PropertyTests.cs
├── Web/                          # Controller and integration tests
│   ├── Controllers/
│   │   ├── BuckTraxControllerTests.cs
│   │   └── ProfilesControllerTests.cs
│   └── ViewModels/
└── Integration/                  # Database integration tests
```

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test category
dotnet test --filter Category=BuckTrax

# Run tests with detailed output
dotnet test --verbosity detailed
```

### Key Test Examples

#### BuckTrax Algorithm Testing
```csharp
[Fact]
public async Task AnalyzeMovementCorridors_WithValidSightings_ShouldGenerateCorridors()
{
    // Arrange
    var sightings = CreateTestSightings();
    var features = CreateTestFeatures();
    var config = GetTestConfiguration();

    // Act
    var corridors = controller.AnalyzeMovementCorridors(sightings, features, config);

    // Assert
    Assert.NotEmpty(corridors);
    Assert.True(corridors.All(c => c.CorridorScore > 0));
    Assert.True(corridors.First().TransitionCount > 0);
}
```

#### Feature Weight Resolution Testing
```csharp
[Fact]
public async Task GetFeatureWeights_WithSeasonalOverrides_ShouldReturnCorrectWeights()
{
    // Arrange
    var propertyId = 1;
    var season = Season.Rut;
    
    // Act
    var weights = await GetFeatureWeights.HandleAsync(db, propertyId, season);
    
    // Assert
    Assert.NotEmpty(weights);
    Assert.Contains(weights, w => w.ClassificationType == ClassificationType.FoodPlot);
}
```

### Testing Data Setup
```csharp
public class TestDataBuilder
{
    public static Profile CreateTestProfile(int propertyId = 1, int tagId = 1)
    {
        return new Profile("Test Buck", propertyId, tagId, ProfileStatus.Active);
    }

    public static List<BuckTraxSighting> CreateTestSightings(int count = 10)
    {
        var sightings = new List<BuckTraxSighting>();
        var baseDate = DateTime.UtcNow.Date;
        
        for (int i = 0; i < count; i++)
        {
            sightings.Add(new BuckTraxSighting
            {
                DateTaken = baseDate.AddHours(i * 2),
                Latitude = 40.0 + (i * 0.001),
                Longitude = -74.0 + (i * 0.001),
                CameraId = i % 3 + 1 // Cycle through cameras
            });
        }
        
        return sightings.OrderBy(s => s.DateTaken).ToList();
    }
}
```

---

## 🔧 Configuration Management

### Application Settings Structure

#### Core Configuration (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=BuckScience;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;",
    "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=yourstorageaccount;AccountKey=yourkey;"
  },
  "AzureAdB2C": {
    "Instance": "https://yourtenant.b2clogin.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "SignUpSignInPolicyId": "B2C_1_signupsignin",
    "ResetPasswordPolicyId": "B2C_1_passwordreset",
    "EditProfilePolicyId": "B2C_1_profileedit"
  },
  "WeatherAPISettings": {
    "BaseUrl": "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/",
    "APIKey": "your-weather-api-key"
  },
  "WeatherSettings": {
    "LocationRoundingPrecision": 2,
    "EnableGPSExtraction": true,
    "FallbackToCameraLocation": true
  },
  "StripeSettings": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "SubscriptionSettings": {
    "TrialDays": 30,
    "MaxPhotosBasic": 1000,
    "MaxPhotosPro": 10000,
    "MaxCamerasBasic": 5,
    "MaxCamerasPro": 25
  }
}
```

#### Development Overrides (appsettings.Development.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "BuckScience": "Debug"
    }
  },
  "DetailedErrors": true,
  "WeatherAPISettings": {
    "APIKey": "your-development-api-key"
  }
}
```

### Configuration Classes
```csharp
// Configuration binding classes
public class WeatherApiSettings
{
    public const string SectionName = "WeatherAPISettings";
    public string BaseUrl { get; set; } = string.Empty;
    public string APIKey { get; set; } = string.Empty;
}

public class SubscriptionSettings
{
    public const string SectionName = "SubscriptionSettings";
    public int TrialDays { get; set; } = 30;
    public int MaxPhotosBasic { get; set; } = 1000;
    public int MaxPhotosPro { get; set; } = 10000;
    public int MaxCamerasBasic { get; set; } = 5;
    public int MaxCamerasPro { get; set; } = 25;
}
```

### Dependency Injection Setup
```csharp
// In Program.cs
builder.Services.Configure<WeatherApiSettings>(
    builder.Configuration.GetSection(WeatherApiSettings.SectionName));
builder.Services.Configure<SubscriptionSettings>(
    builder.Configuration.GetSection(SubscriptionSettings.SectionName));

// Usage in controllers/services
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

## 🚢 Deployment Guide

### Prerequisites
- **Azure App Service** or **IIS Server**
- **SQL Server** with spatial data support
- **Azure Storage Account** for photos
- **SSL Certificate** for HTTPS

### Database Deployment
```bash
# Generate SQL scripts for production deployment
dotnet ef migrations script --context AppDbContext --output deploy.sql

# Or use direct deployment to target database
dotnet ef database update --context AppDbContext --connection "your-production-connection-string"
```

### Application Deployment

#### Azure App Service
```bash
# Publish for Azure
dotnet publish src/BuckScience.Web -c Release -o ./publish

# Deploy to Azure (using Azure CLI)
az webapp deployment source config-zip -g ResourceGroupName -n AppName --src ./publish.zip
```

#### Docker Deployment
```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY ./publish .
ENTRYPOINT ["dotnet", "BuckScience.Web.dll"]
```

### Environment Configuration
```bash
# Set environment variables in production
export ConnectionStrings__DefaultConnection="your-production-connection"
export AzureAdB2C__ClientId="your-production-client-id"
export WeatherAPISettings__APIKey="your-production-api-key"
```

---

## 🔍 Debugging & Troubleshooting

### Common Development Issues

#### 1. Spatial Data Issues
**Problem**: Geometry/Geography errors in Entity Framework
```csharp
// Solution: Ensure proper SRID configuration
modelBuilder.Entity<Property>()
    .Property(p => p.Center)
    .HasColumnType("geometry")
    .HasSrid(4326);  // WGS84

// Create points with correct SRID
var point = new Point(longitude, latitude) { SRID = 4326 };
```

#### 2. Authentication Issues
**Problem**: Azure AD B2C authentication failures
```csharp
// Check configuration in Program.cs
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
});

// Verify appsettings values
"AzureAdB2C": {
    "Instance": "https://yourtenant.b2clogin.com/",
    "Domain": "yourtenant.onmicrosoft.com",
    "TenantId": "correct-tenant-id",
    "ClientId": "correct-client-id"
}
```

#### 3. BuckTrax Prediction Issues
**Problem**: No corridors generated or limited data warnings
```csharp
// Debug sighting collection
var sightings = await GetProfileSightings(profileId, tagId, propertyId, ct);
Console.WriteLine($"Found {sightings.Count} sightings");

// Check data quality thresholds
var config = GetConfiguration();
if (sightings.Count < config.MinimumSightingsThreshold)
{
    // Increase data collection or lower thresholds for testing
}
```

### Logging Configuration
```csharp
// Program.cs - Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Use structured logging
_logger.LogInformation("Processing prediction for profile {ProfileId} with {SightingCount} sightings", 
    profileId, sightings.Count);
```

### Database Debugging
```sql
-- Check entity relationships
SELECT p.Name, COUNT(ph.Id) as PhotoCount, COUNT(DISTINCT c.Id) as CameraCount
FROM Profiles p
LEFT JOIN PhotoTags pt ON p.TagId = pt.TagId
LEFT JOIN Photos ph ON pt.PhotoId = ph.Id
LEFT JOIN Cameras c ON ph.CameraId = c.Id
WHERE p.Id = @ProfileId
GROUP BY p.Name;

-- Verify spatial data
SELECT Id, Name, Center.STAsText() as CenterWKT, Center.STSrid as SRID
FROM Properties
WHERE ApplicationUserId = @UserId;
```

---

## 🔒 Security Best Practices

### Authentication & Authorization
```csharp
// Always validate user ownership
if (_currentUser.Id is null) return Forbid();

// Ensure data access is properly scoped
var profile = await _db.Profiles
    .Join(_db.Properties, p => p.PropertyId, prop => prop.Id, (p, prop) => new { Profile = p, Property = prop })
    .Where(x => x.Profile.Id == profileId && x.Property.ApplicationUserId == userId)
    .FirstOrDefaultAsync(ct);

if (profile == null) return NotFound(); // Or Forbid() for security
```

### Input Validation
```csharp
// Validate all user inputs
public class BuckTraxPredictionRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public int ProfileId { get; set; }
    
    public Season? Season { get; set; }
    
    [Range(0, 23)]
    public int? TimeOfDayFilter { get; set; }
}
```

### Data Protection
```csharp
// Use parameterized queries (EF Core handles this automatically)
var photos = await _db.Photos
    .Where(p => p.CameraId == cameraId && p.DateTaken >= startDate)
    .ToListAsync();

// Sanitize file uploads
public bool IsValidImageFile(IFormFile file)
{
    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    return allowedExtensions.Contains(extension);
}
```

---

## 📈 Performance Optimization

### Database Optimization
```csharp
// Use AsNoTracking for read-only queries
var properties = await _db.Properties
    .AsNoTracking()
    .Where(p => p.ApplicationUserId == userId)
    .ToListAsync();

// Optimize joins and projections
var profiles = await _db.Profiles
    .Where(p => p.PropertyId == propertyId)
    .Select(p => new ProfileViewModel
    {
        Id = p.Id,
        Name = p.Name,
        TagName = p.Tag.TagName  // Include only needed data
    })
    .ToListAsync();
```

### Caching Strategies
```csharp
// Cache expensive feature weight calculations
private readonly IMemoryCache _cache;

public async Task<List<FeatureWeight>> GetFeatureWeightsAsync(int propertyId, Season season)
{
    var cacheKey = $"feature-weights-{propertyId}-{season}";
    
    if (!_cache.TryGetValue(cacheKey, out List<FeatureWeight> weights))
    {
        weights = await LoadFeatureWeights(propertyId, season);
        _cache.Set(cacheKey, weights, TimeSpan.FromMinutes(30));
    }
    
    return weights;
}
```

### Async Best Practices
```csharp
// Always use ConfigureAwait(false) in library code
public async Task<Result> ProcessDataAsync()
{
    var data = await LoadDataAsync().ConfigureAwait(false);
    var processed = await ProcessAsync(data).ConfigureAwait(false);
    return processed;
}

// Use CancellationToken for long-running operations
public async Task<List<Corridor>> AnalyzeCorridorsAsync(
    List<Sighting> sightings, 
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    
    var results = new List<Corridor>();
    foreach (var sighting in sightings)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Process sighting
    }
    
    return results;
}
```

---

## 🔧 Extending the System

### Adding New Features

#### 1. New Entity Creation
```csharp
// 1. Create domain entity
public class HuntingPressure
{
    public int Id { get; private set; }
    public int PropertyId { get; private set; }
    public DateTime Date { get; private set; }
    public int IntensityLevel { get; private set; } // 1-10 scale
    
    public void UpdateIntensity(int newLevel)
    {
        if (newLevel < 1 || newLevel > 10)
            throw new ArgumentOutOfRangeException(nameof(newLevel));
        IntensityLevel = newLevel;
    }
}

// 2. Add to DbContext
public DbSet<HuntingPressure> HuntingPressures => Set<HuntingPressure>();

// 3. Create migration
dotnet ef migrations add AddHuntingPressure --context AppDbContext
```

#### 2. New Application Service
```csharp
// Application layer service
public static class GetHuntingPressure
{
    public record Result(int PropertyId, DateTime Date, int IntensityLevel);
    
    public static async Task<List<Result>> HandleAsync(
        IAppDbContext db,
        int propertyId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct = default)
    {
        return await db.HuntingPressures
            .AsNoTracking()
            .Where(hp => hp.PropertyId == propertyId 
                      && hp.Date >= startDate.ToDateTime(TimeOnly.MinValue)
                      && hp.Date <= endDate.ToDateTime(TimeOnly.MaxValue))
            .Select(hp => new Result(hp.PropertyId, hp.Date, hp.IntensityLevel))
            .ToListAsync(ct);
    }
}
```

#### 3. Controller Integration
```csharp
[HttpGet]
[Route("/api/properties/{propertyId}/hunting-pressure")]
public async Task<IActionResult> GetHuntingPressure(
    int propertyId,
    [FromQuery] DateOnly startDate,
    [FromQuery] DateOnly endDate,
    CancellationToken ct)
{
    if (_currentUser.Id is null) return Forbid();
    
    // Validate property ownership
    var hasAccess = await _db.Properties
        .AnyAsync(p => p.Id == propertyId && p.ApplicationUserId == _currentUser.Id.Value, ct);
    
    if (!hasAccess) return Forbid();
    
    var pressureData = await GetHuntingPressure.HandleAsync(_db, propertyId, startDate, endDate, ct);
    return Json(pressureData);
}
```

### Integrating with BuckTrax

#### Extend Prediction Algorithm
```csharp
// Modify GenerateMovementPredictions to include hunting pressure
public async Task<BuckTraxPredictionResult> GenerateMovementPredictions(
    int profileId, 
    Season? season, 
    CancellationToken ct)
{
    // ... existing code ...
    
    // NEW: Get hunting pressure data
    var pressureData = await GetHuntingPressure.HandleAsync(
        _db, profile.PropertyId, 
        DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), 
        DateOnly.FromDateTime(DateTime.UtcNow), 
        ct);
    
    // Modify corridor scoring based on hunting pressure
    foreach (var corridor in movementCorridors)
    {
        corridor.CorridorScore *= CalculatePressureModifier(pressureData, corridor);
    }
    
    // ... rest of method ...
}

private double CalculatePressureModifier(List<GetHuntingPressure.Result> pressureData, BuckTraxMovementCorridor corridor)
{
    if (!pressureData.Any()) return 1.0; // No pressure data, no modification
    
    var averagePressure = pressureData.Average(p => p.IntensityLevel);
    
    // Reduce corridor scores in high-pressure areas
    return averagePressure switch
    {
        >= 8 => 0.5,  // High pressure - 50% reduction
        >= 6 => 0.7,  // Medium pressure - 30% reduction  
        >= 4 => 0.9,  // Low pressure - 10% reduction
        _ => 1.0      // No pressure impact
    };
}
```

### Custom Analytics Integration

#### Create Custom Chart Data
```csharp
public class CustomAnalyticsService
{
    public async Task<ChartData> GetHuntingPressureChart(int propertyId, CancellationToken ct)
    {
        var pressureData = await _db.HuntingPressures
            .Where(hp => hp.PropertyId == propertyId)
            .GroupBy(hp => hp.Date.Date)
            .Select(g => new { Date = g.Key, AveragePressure = g.Average(hp => hp.IntensityLevel) })
            .OrderBy(x => x.Date)
            .ToListAsync(ct);
        
        return new ChartData
        {
            ChartType = "line",
            DataPoints = pressureData.Select(p => new ChartDataPoint
            {
                Label = p.Date.ToString("MMM dd"),
                Value = p.AveragePressure,
                Metadata = new { Date = p.Date }
            }).ToList()
        };
    }
}
```

---

## 📚 Additional Resources

### External Documentation
- **ASP.NET Core**: https://docs.microsoft.com/en-us/aspnet/core/
- **Entity Framework Core**: https://docs.microsoft.com/en-us/ef/core/
- **NetTopologySuite**: https://nettopologysuite.github.io/NetTopologySuite/
- **Azure AD B2C**: https://docs.microsoft.com/en-us/azure/active-directory-b2c/
- **Mapbox**: https://docs.mapbox.com/

### Code Examples Repository
```bash
# Clone examples repository
git clone https://github.com/buckscience/BuckScienceExamples.git

# Contains:
# - Sample data sets
# - Algorithm extensions
# - Custom analytics examples
# - Performance testing scripts
```

### Community & Support
- **GitHub Issues**: Report bugs and request features
- **Documentation**: Comprehensive guides in `/docs` folder
- **Code Comments**: Inline documentation throughout codebase
- **Unit Tests**: Examples and patterns in test suite

---

*This developer guide provides comprehensive information for extending and maintaining the BuckScience application. For specific implementation questions, refer to the existing codebase patterns and comprehensive test suite.*