# BuckScience Application

## 🦌 Overview

BuckScience is a comprehensive hunting analytics platform designed to help hunters, land managers, and wildlife researchers optimize their hunting strategies through data-driven insights. The application leverages trail camera photos, weather data, property features, and advanced movement prediction algorithms to provide actionable intelligence for deer hunting success.

### Core Purpose
Transform raw hunting data (trail camera photos, property features, weather conditions) into intelligent insights that improve hunting success rates and land management decisions.

## 🏗️ Architecture

BuckScience follows Clean Architecture principles with clear separation of concerns:

```
├── src/
│   ├── BuckScience.Domain/           # Core business entities and rules
│   ├── BuckScience.Application/      # Business logic and use cases
│   ├── BuckScience.Infrastructure/   # External dependencies (DB, APIs)
│   ├── BuckScience.Web/             # MVC web application
│   ├── BuckScience.API/             # API endpoints (if used)
│   └── BuckScience.Shared/          # Shared utilities and configuration
├── tests/
│   └── BuckScience.Tests/           # Comprehensive test suite (421+ tests)
└── docs/                            # Technical documentation
```

### Technology Stack
- **Backend**: ASP.NET Core 8.0, Entity Framework Core
- **Database**: SQL Server with spatial data support (NetTopologySuite)
- **Frontend**: MVC with Razor views, JavaScript, Mapbox for mapping
- **Authentication**: Azure AD B2C integration
- **External APIs**: VisualCrossing Weather API, Azure Blob Storage
- **Testing**: xUnit, comprehensive test coverage

## 🎯 Key Features

### 1. **BuckTrax - Movement Prediction System**
Advanced deer movement corridor prediction using historical sightings and property features.
- **Profile-based analysis** for individual deer tracking
- **Feature-aware routing** through logical waypoints (creek crossings, food plots, etc.)
- **Time-based segmentation** (Early Morning, Morning, Midday, Evening, Night)
- **Corridor scoring** based on transition frequency and feature weights
- **Limited data warnings** for insufficient sample sizes
- **Interactive mapping** with Mapbox visualization

### 2. **BuckLens Analytics**
Comprehensive data visualization and pattern analysis for deer sightings.
- **Weather correlation** analysis (temperature, wind, moon phase)
- **Time-of-day patterns** with interactive charts
- **Camera performance** tracking and optimization
- **Best odds analysis** for optimal hunting conditions
- **Data export** capabilities (CSV/JSON)

### 3. **Property Management**
Complete property and camera management system.
- **Spatial property mapping** with boundaries and features
- **Camera placement tracking** with historical records
- **Feature classification** (food plots, water sources, travel corridors, etc.)
- **Feature weight management** for movement prediction
- **Season-specific configurations**

### 4. **Photo Management & Tagging**
Automated photo processing and organization system.
- **Bulk photo upload** with EXIF data extraction
- **Automated weather integration** during upload
- **Intelligent tagging system** for deer identification
- **Profile-based organization** for individual deer tracking
- **Camera-photo association** with location verification

### 5. **Weather Integration**
Comprehensive weather data integration for pattern analysis.
- **Batch processing optimization** to minimize API calls
- **Historical weather correlation** with sighting data
- **Location-based weather lookup** with coordinate rounding
- **Automated weather assignment** during photo upload

### 6. **Subscription Management**
Flexible subscription system with usage limits.
- **Trial accounts** with limited features
- **Tiered subscriptions** (Basic, Pro, etc.)
- **Usage tracking** (photos, cameras, properties)
- **Stripe payment integration**

## 🚀 Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server (with spatial data support)
- Azure AD B2C tenant (for authentication)
- VisualCrossing Weather API key
- Azure Blob Storage account

### Configuration

1. **Database Connection**
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=...;Database=BuckScience;..."
   }
   ```

2. **Authentication (Azure AD B2C)**
   ```json
   "AzureAdB2C": {
     "Instance": "https://your-tenant.b2clogin.com/",
     "Domain": "your-tenant.onmicrosoft.com",
     "TenantId": "your-tenant-id",
     "ClientId": "your-client-id"
   }
   ```

3. **Weather API**
   ```json
   "WeatherAPISettings": {
     "BaseUrl": "https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/",
     "APIKey": "your-api-key"
   }
   ```

4. **Storage**
   ```json
   "ConnectionStrings": {
     "StorageConnectionString": "DefaultEndpointsProtocol=https;AccountName=..."
   }
   ```

### Setup & Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/buckscience/BuckScienceApp.git
   cd BuckScienceApp
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Update database**
   ```bash
   dotnet ef database update --context AppDbContext
   ```

4. **Configure settings**
   - Copy `appsettings.Development.template.json` to `appsettings.Development.json`
   - Update with your API keys and connection strings

5. **Run the application**
   ```bash
   dotnet run --project src/BuckScience.Web
   ```

### Development Workflow

1. **Build & Test**
   ```bash
   dotnet build
   dotnet test
   ```

2. **Database Migrations**
   ```bash
   dotnet ef migrations add MigrationName --context AppDbContext
   dotnet ef database update --context AppDbContext
   ```

## 📊 Data Flow & Architecture

### Core Entities
- **Properties** - Hunting properties with spatial boundaries
- **Cameras** - Trail cameras with placement history
- **Photos** - Trail camera images with EXIF data and weather
- **Profiles** - Individual deer identification and tracking
- **PropertyFeatures** - Spatial features (food plots, water, travel corridors)
- **FeatureWeights** - Seasonal weights for movement prediction
- **Weather** - Historical weather data for correlation analysis

### Key Workflows

1. **Photo Upload & Processing**
   ```
   Upload → EXIF Extraction → Weather Lookup → Tagging → Profile Association
   ```

2. **Movement Prediction**
   ```
   Profile Selection → Sighting Analysis → Feature Association → Corridor Calculation → Visualization
   ```

3. **Analytics Generation**
   ```
   Data Collection → Pattern Analysis → Chart Generation → Best Odds Calculation
   ```

## 🧪 Testing

The application includes comprehensive testing with 421+ tests covering:
- **Unit Tests** - Business logic and domain entities
- **Integration Tests** - Database operations and API endpoints
- **Controller Tests** - MVC actions and API responses
- **Service Tests** - External API integrations and complex algorithms

Run all tests:
```bash
dotnet test --verbosity normal
```

## 📁 Documentation

### Technical Documentation
- **[BuckTrax Deep Dive](docs/BUCKTRAX_ENHANCED.md)** - Complete BuckTrax system documentation
- **[BuckLens Analytics](docs/BUCKLENS_ANALYTICS.md)** - Analytics module documentation
- **[Weather Integration](docs/WEATHER_INTEGRATION.md)** - Weather API integration details
- **[Feature Weights](docs/FeatureWeightHybridSeasonMapping.md)** - Feature weight system
- **[Photo Management](docs/PHOTO_FILTERING.md)** - Photo processing and filtering

### API Documentation
- **BuckTrax API** - Movement prediction endpoints
- **Analytics API** - Chart data and summary endpoints
- **Property API** - Property and feature management
- **Photo API** - Upload and tagging endpoints

## 🔧 Development Notes

### Code Organization
- **Clean Architecture** - Clear separation between layers
- **CQRS Pattern** - Command/Query separation in application layer
- **Repository Pattern** - Data access abstraction
- **Dependency Injection** - Comprehensive DI configuration

### Key Design Decisions
- **Spatial Data** - NetTopologySuite for geographic calculations
- **Profile-Scoped Analysis** - All analytics tied to individual deer profiles
- **Batch Processing** - Optimized weather API calls and photo processing
- **Feature-Aware Algorithms** - Intelligence routing through property features

### Performance Considerations
- **Database Indexing** - Optimized for common query patterns
- **Spatial Queries** - Efficient coordinate-based lookups
- **Caching Strategy** - Feature weights and configuration caching
- **Lazy Loading** - On-demand data loading for large datasets

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes with appropriate tests
4. Ensure all tests pass (`dotnet test`)
5. Commit your changes (`git commit -m 'Add amazing feature'`)
6. Push to the branch (`git push origin feature/amazing-feature`)
7. Open a Pull Request

### Coding Standards
- Follow Clean Architecture principles
- Maintain comprehensive test coverage
- Use meaningful commit messages
- Document complex algorithms and business logic

## 📋 Roadmap

### Planned Features
- **Machine Learning Integration** - Advanced pattern recognition
- **Real-time Updates** - Live prediction updates
- **Mobile Application** - Field companion app
- **Advanced Analytics** - Predictive modeling improvements
- **Multi-property Analysis** - Cross-property pattern analysis

### Technical Improvements
- **Performance Optimization** - Query optimization and caching
- **Background Processing** - Async job processing for heavy operations
- **API Enhancement** - RESTful API expansion
- **Testing Coverage** - Expanded integration and E2E testing

---

## 📞 Support

For technical support or questions:
- **Documentation** - Check the `/docs` folder for detailed technical docs
- **Issues** - Create GitHub issues for bugs or feature requests
- **Testing** - Run `dotnet test` to verify your environment setup

---

*BuckScience - Transforming hunting data into intelligence.*