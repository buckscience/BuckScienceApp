# BuckTrax Deep Dive: Complete Technical Documentation

## 🎯 Executive Summary

BuckTrax is an advanced deer movement prediction system that transforms trail camera sightings into intelligent movement corridor forecasts. This comprehensive system analyzes historical deer sighting patterns, integrates property features, and leverages sophisticated algorithms to predict optimal hunting locations and timing.

### Key Capabilities
- **Profile-Based Prediction**: Individual deer tracking and movement analysis
- **Feature-Aware Routing**: Intelligent pathfinding through natural waypoints
- **Time-Segmented Analysis**: Dynamic time period optimization based on property daylight hours
- **Corridor Scoring**: Mathematical ranking of movement routes by frequency and feature importance
- **Interactive Visualization**: Real-time mapping with Mapbox integration
- **Data Quality Validation**: Automatic warning system for insufficient data

---

## 🏗️ System Architecture

### Component Overview
```
BuckTraxController (1,226 lines)
├── Index View                    # Primary user interface
├── API Endpoints                 # REST endpoints for data
├── Movement Prediction Engine    # Core algorithm implementation
├── Feature Integration          # Property feature analysis
├── Time Segmentation           # Dynamic time period calculation
└── Visualization Support       # Map data preparation
```

### Data Flow Architecture
```
User Selection → Profile Validation → Sighting Analysis → Feature Association → 
Movement Detection → Corridor Scoring → Time Segmentation → Visualization
```

---

## 🔧 Core Implementation

### 1. Controller Structure
**File**: `src/BuckScience.Web/Controllers/BuckTraxController.cs` (1,226 lines)

#### Primary Endpoints
```csharp
[HttpGet("/bucktrax")]                                    // Main interface
[HttpGet("/bucktrax/api/properties/{propertyId}/profiles")]  // Profile loading
[HttpGet("/bucktrax/api/configuration")]                 // System configuration
[HttpPost("/bucktrax/api/predict")]                      // Movement prediction
```

#### Key Dependencies
- `IAppDbContext` - Database access for entities
- `ICurrentUserService` - User authentication and authorization
- `GetProfile.HandleAsync()` - Profile validation and retrieval
- `GetFeatureWeights.HandleAsync()` - Feature weight resolution

### 2. Movement Prediction Algorithm

#### Core Method: `GenerateMovementPredictions()`
**Location**: Lines 122-250

**Algorithm Flow**:
```csharp
1. Profile & Property Data Retrieval
   └── Load profile with property relationship
   └── Validate user ownership through property association

2. Sighting Collection & Processing
   └── GetProfileSightings() - Photo-based sighting extraction
   └── GroupPhotosIntoSightings() - 15-minute proximity clustering
   └── Chronological ordering for movement sequence

3. Feature Integration
   └── GetPropertyFeaturesWithWeights() - Load with seasonal weights
   └── AssociateSightingsWithFeatures() - Spatial proximity matching (100m default)

4. Movement Corridor Analysis
   └── AnalyzeMovementCorridors() - Sequential sighting analysis
   └── Route identification with feature-aware pathfinding
   └── Corridor scoring with weighted calculations

5. Time Segmentation
   └── Property-specific daylight hour calculation
   └── Dynamic time period generation (typically 3 segments)
   └── Segment-specific corridor filtering

6. Quality Validation
   └── Data sufficiency thresholds (10+ sightings, 3+ transitions)
   └── Limited data warnings and confidence scoring
```

#### Sighting Processing Logic
**Method**: `GetProfileSightings()`
**Location**: Lines 251-285

```csharp
// Complex query joining multiple entities
var sightingsQuery = from photo in _db.Photos
    join photoTag in _db.PhotoTags on photo.Id equals photoTag.PhotoId
    join camera in _db.Cameras on photo.CameraId equals camera.Id
    join placementHistory in _db.CameraPlacementHistories on camera.Id equals placementHistory.CameraId
    where photoTag.TagId == tagId 
          && camera.PropertyId == propertyId
          && placementHistory.StartDate <= photo.DateTaken
          && (placementHistory.EndDate == null || placementHistory.EndDate >= photo.DateTaken)
    select new BuckTraxSighting { /* ... */ };
```

**Key Features**:
- **Historical Accuracy**: Uses `CameraPlacementHistory` for precise camera locations at photo time
- **GPS Integration**: Prefers photo GPS coordinates, falls back to camera location
- **Weather Association**: Links each sighting to corresponding weather data
- **Chronological Ordering**: Ensures proper sequence for movement analysis

#### Photo Grouping Algorithm
**Method**: `GroupPhotosIntoSightings()`
**Location**: Lines 287-330

**Clustering Logic**:
```csharp
// 15-minute time window for grouping
if (timeDiff.TotalMinutes > 15)
{
    sightings.Add(currentSighting);
    currentSighting = photo;
    currentSightingStartTime = photo.DateTaken;
}
```

**Purpose**: 
- Reduces noise from rapid-fire camera triggering
- Creates meaningful movement events from multiple photos
- Improves algorithm accuracy by focusing on actual location changes

### 3. Movement Corridor Analysis

#### Core Method: `AnalyzeMovementCorridors()`
**Location**: Lines 331-421

**Advanced Features**:

##### A. Movement Route Identification
**Method**: `IdentifyMovementRoutes()`
**Location**: Lines 423-588

```csharp
// Time window and distance constraints
var timeDiff = nextSighting.DateTaken - lastPoint.VisitTime;
if (timeDiff.TotalMinutes > config.MovementTimeWindowMinutes) // Default: 480 min (8 hours)
    break;

var distance = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude,
                                nextSighting.Latitude, nextSighting.Longitude);
if (distance > config.MaxMovementDistanceMeters) // Default: 5000m (5km)
    continue;
```

##### B. Feature-Aware Routing Enhancement
**Method**: `CreateFeatureAwareRoute()`
**Location**: Lines 638-759

**Intelligence Layer**:
- **Waypoint Detection**: Identifies logical intermediate features (creek crossings, pinch points, food plots)
- **Distance Optimization**: Only applies to movements >200m to avoid over-optimization
- **Detour Limits**: Maximum 30% longer than direct path
- **Feature Preference**: Prioritizes higher-weighted features as waypoints

```csharp
// Feature-aware routing logic
if (distance >= config.MinimumDistanceForFeatureRouting) // Default: 200m
{
    var candidateFeatures = features.Where(f => 
        IsWithinRoutingCorridor(directPath, f.Latitude, f.Longitude, 200) &&
        f.EffectiveWeight > 0.3 && // Only high-value features
        IsLogicalWaypoint(f.ClassificationName)
    ).OrderByDescending(f => f.EffectiveWeight);
}
```

##### C. Movement Barriers Detection
**Method**: `IsMovementBlocked()`
**Location**: Lines 848-867

**Intelligent Filtering**:
- **Straight-line Detection**: Flags unnatural movements that may cross roads/highways
- **Water Barrier Analysis**: Prevents impossible water crossings (unless at designated crossings)
- **Distance Validation**: Blocks unrealistic long-distance movements

#### Corridor Scoring System
**Method**: `CalculateCorridorScore()`
**Location**: Lines 793-802

**Mathematical Formula**:
```csharp
// Weighted scoring with amplification
var weightMultiplier = (startWeight + endWeight) / 2;
var amplifiedWeight = Math.Max(0.5, Math.Pow(weightMultiplier, 1.5));
return transitionCount * amplifiedWeight * 5;
```

**Scoring Components**:
- **Transition Frequency**: Raw count of movements through corridor
- **Feature Weight Integration**: Average of start/end feature weights
- **Amplification Factor**: Exponential scaling (1.5 power) to emphasize high-value features
- **Base Multiplier**: 5x multiplier for readable scoring scale

### 4. Time Segmentation System

#### Dynamic Time Period Calculation
**Location**: Lines 171-238

**Property-Specific Logic**:
```csharp
// Calculate daylight span and split into thirds
var dayStart = profile.Property.DayHour;     // Property-specific dawn
var dayEnd = profile.Property.NightHour;     // Property-specific dusk

if (dayEnd > dayStart)
{
    daylightSpan = dayEnd - dayStart;
}
else
{
    // Handle midnight-spanning scenarios
    daylightSpan = (24 - dayStart) + dayEnd;
}

var segmentDuration = daylightSpan / 3; // Three equal segments
```

**Time Segments Generated**:
1. **Early Period**: Dawn to Dawn + segmentDuration
2. **Mid Period**: Early End to Early End + segmentDuration  
3. **Late Period**: Mid End to Dusk

#### Time Pattern Analysis
**Method**: `CalculateTimeOfDayPattern()`
**Location**: Lines 804-846

**Pattern Detection**:
- **Transition Time Collection**: Gathers all movement times for specific corridors
- **Statistical Analysis**: Identifies dominant time periods (>30% threshold)
- **Pattern Classification**: Maps to human-readable time categories

```csharp
// Pattern threshold analysis
if (transitionTimes.Count(t => t >= 5 && t < 8) > transitionTimes.Count * 0.3)
    patterns.Add("Early Morning");
if (transitionTimes.Count(t => t >= 8 && t < 12) > transitionTimes.Count * 0.3)
    patterns.Add("Morning");
// ... continues for all time periods
```

#### Corridor-Time Segment Matching
**Method**: `IsCorridorActiveInTimeSegment()`
**Location**: Lines 1034-1077

**Intelligent Filtering**:
- **Pattern Matching**: Links corridor activity patterns to current time segment
- **Flexible Range Matching**: Works with dynamic property-specific time segments
- **Overlap Detection**: Handles midnight-spanning segments appropriately

### 5. Feature Integration System

#### Feature Weight Resolution
**Integration Point**: `GetFeatureWeights.HandleAsync()`

**Weight Priority Hierarchy**:
1. **Individual Feature Weight** (`PropertyFeature.Weight`)
2. **Seasonal Weight Override** (`FeatureWeight.SeasonalWeights`)
3. **Property Custom Weight** (`FeatureWeight.UserWeight`)
4. **System Default Weight** (`FeatureWeight.DefaultWeight`)

#### Spatial Association
**Method**: `AssociateSightingsWithFeatures()`
**Location**: Referenced but implementation in application layer

**Association Logic**:
- **Proximity Matching**: 100-meter radius (configurable)
- **Spatial Calculations**: Uses geodetic distance calculations
- **Fallback Handling**: Camera location used when no features in proximity

### 6. Data Quality & Validation

#### Threshold System
**Configuration Object**: `BuckTraxConfiguration`

```csharp
{
    MovementTimeWindowMinutes: 480,        // 8-hour movement window
    MaxMovementDistanceMeters: 5000,       // 5km maximum realistic movement
    CameraFeatureProximityMeters: 100,     // Feature association radius
    MinimumSightingsThreshold: 10,         // Data quality threshold
    MinimumTransitionsThreshold: 3,        // Movement validation threshold
    ShowLimitedDataWarning: true,          // User warning display
    
    // Feature-Aware Routing Configuration
    EnableFeatureAwareRouting: true,
    MinimumDistanceForFeatureRouting: 200,
    MaximumDetourPercentage: 0.3,
    MaximumWaypointsPerRoute: 2
}
```

#### Confidence Scoring
**Method**: `CalculateConfidenceScore()`
**Location**: Lines 1021-1032

**Multi-Factor Analysis**:
```csharp
var proportion = (double)segmentSightings / totalSightings;
var dataConfidence = Math.Min(segmentSightings / 10.0, 1.0);  // Sample size confidence
var proportionConfidence = proportion;                         // Segment activity level
var corridorBonus = Math.Min(corridorCount / 5.0, 0.2);      // Movement pattern bonus

var baseScore = (dataConfidence * 0.6 + proportionConfidence * 0.4) * 100;
return Math.Round(Math.Min(baseScore + (corridorBonus * 100), 100), 1);
```

---

*This represents the first part of the comprehensive BuckTrax documentation. The system implements sophisticated spatial analysis, temporal pattern recognition, and intelligent routing algorithms for wildlife movement prediction.*
