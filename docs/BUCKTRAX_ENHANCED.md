# BuckTrax Enhanced: Profile-Based Corridor Movement Prediction

## Overview

BuckTrax has been enhanced with advanced corridor movement prediction capabilities that analyze deer movement patterns using historical sightings and property features. The system provides profile-scoped analysis with feature weight integration and configurable thresholds for prediction validity.

## Key Features

### 1. Profile-Scoped Analysis
- All analysis runs exclusively for the selected profile (individual deer)
- Uses profile ID as "individual id" in all calculations
- No cross-profile or global analytics to maintain data integrity
- Ensures privacy and accuracy of individual tracking

### 2. Corridor Inference Algorithm
The enhanced algorithm analyzes movement patterns through:

#### Sighting Sequence Analysis
- Chronologically sequences all sightings for the selected profile
- Identifies movement transitions between camera locations
- Applies configurable time window and distance constraints

#### Feature Association
- Associates each camera with its nearest property feature
- Uses configurable proximity threshold for feature-camera relationships
- Records transitions as feature-to-feature movements

#### **NEW: Feature-Aware Routing**
- Intelligently routes through logical intermediate features instead of direct camera-to-camera paths
- Identifies waypoints like creek crossings, pinch points, food plots, and travel corridors
- Only applies to longer movements where intermediate features make logical sense
- Configurable with distance thresholds and detour percentages
- Maintains original camera-to-camera routing when no logical features exist

#### Transition Aggregation
- Counts movement frequencies between feature pairs
- Calculates corridor scores using feature weights
- Identifies time-of-day movement patterns

### 3. Feature Weights Integration
The system integrates with the existing feature weight system:

#### Weight Resolution Priority
1. **Individual Feature Weight** (PropertyFeature.Weight)
2. **Seasonal Weight Override** (FeatureWeight.SeasonalWeights)
3. **Property Custom Weight** (FeatureWeight.UserWeight)
4. **System Default Weight** (FeatureWeight.DefaultWeight)

#### Corridor Scoring Formula
```
CorridorScore = TransitionCount * (StartFeatureWeight + EndFeatureWeight) / 2
```

### 4. Threshold for Prediction Validity
Configurable thresholds ensure data quality:

#### Threshold Parameters
- **MinimumSightingsThreshold**: Default 10 sightings
- **MinimumTransitionsThreshold**: Default 3 transitions
- **ShowLimitedDataWarning**: Default true

#### Limited Data Handling
When data falls below thresholds:
- UI displays warning: "Due to limited data, the predictive model is extremely limited."
- Analytics clearly indicate limited confidence
- Predictions still shown but with appropriate caveats

### 5. Enhanced Visualization and Analytics

#### Map Visualization
- **Movement Corridors**: Green lines connecting features, thickness indicates frequency
- **Sighting Zones**: Red circles for historical locations
- **Corridor Predictions**: Orange circles for feature-based predictions
- **Interactive Popups**: Detailed information on click

#### Analytics Panel
- **Total Sightings**: Historical sighting count for profile
- **Total Transitions**: Movement transitions identified
- **Active Periods**: Time segments with activity
- **Corridor Information**: Transition counts, scores, distances
- **Feature Weights**: Display effective weights used in calculations

### 6. Configuration Options

#### Movement Analysis Parameters
```javascript
{
    MovementTimeWindowMinutes: 480,        // 8 hours
    MaxMovementDistanceMeters: 5000,       // 5 km
    CameraFeatureProximityMeters: 100,     // 100 meters
    MinimumSightingsThreshold: 10,
    MinimumTransitionsThreshold: 3,
    ShowLimitedDataWarning: true,
    
    // NEW: Feature-Aware Routing Configuration
    EnableFeatureAwareRouting: true,        // Enable intelligent feature routing
    MinimumDistanceForFeatureRouting: 200,  // Minimum distance to use feature routing
    MaximumDetourPercentage: 0.3,          // Max 30% longer route allowed
    MaximumWaypointsPerRoute: 2             // Limit waypoints per route
}
```

#### Feature-Aware Routing
The system now intelligently routes through logical features instead of direct camera-to-camera paths:
- **Waypoint Features**: Creek crossings, pinch points, food plots, travel corridors, ridges, fence crossings
- **Distance Logic**: Short movements (<200m) use direct routing, longer movements consider features
- **Path Optimization**: Routes stay within 200m of direct path and max 30% longer distance
- **Feature Weighting**: Prefers higher-weighted features as waypoints
- **Fallback Behavior**: Uses original camera-to-camera routing when no logical features exist

#### Time-of-Day Analysis
- **Early Morning**: 5:00-8:00 AM
- **Morning**: 8:00-11:00 AM
- **Midday**: 11:00 AM-2:00 PM
- **Afternoon**: 2:00-5:00 PM
- **Evening**: 5:00-8:00 PM
- **Night**: 8:00 PM-5:00 AM

### 7. API Endpoints

#### Get Configuration
```
GET /bucktrax/api/configuration
```
Returns current configuration parameters.

#### Generate Predictions
```
POST /bucktrax/api/predict
{
    "profileId": 123,
    "season": "Fall",          // Optional
    "timeOfDayFilter": 8       // Optional hour filter
}
```

## Technical Implementation

### Data Flow
1. **Profile Selection**: User selects individual deer profile
2. **Data Retrieval**: System fetches profile-specific sightings and features
3. **Feature Association**: Cameras associated with nearest features
4. **Corridor Analysis**: Sequential sightings analyzed for movement patterns
5. **Weight Integration**: Feature weights applied to calculate corridor scores
6. **Threshold Validation**: Data quality checked against configured thresholds
7. **Visualization**: Results displayed on interactive map with analytics

### Database Integration
- Leverages existing `Profile`, `Photo`, `Camera`, `PropertyFeature` entities
- Integrates with `FeatureWeight` system for weight resolution
- Uses `CameraPlacementHistory` for accurate location data

### Performance Considerations
- Profile-scoped queries optimize database performance
- Configurable thresholds prevent processing of insufficient data
- Efficient spatial calculations for distance and proximity
- Cached feature weight lookups

## Methodology and Limitations

### Methodology
- **Temporal Analysis**: Movement inferred from chronological sightings
- **Spatial Constraints**: Distance and proximity thresholds ensure realistic movements
- **Weight-Adjusted Scoring**: Feature importance affects corridor significance
- **Time Pattern Recognition**: Identifies peak activity periods for corridors

### Limitations
- **Camera Coverage**: Analysis limited to camera-monitored areas
- **Time Gaps**: Large gaps between sightings may miss actual movements
- **Individual Behavior**: Predictions based on historical patterns may not predict new behaviors
- **Environmental Factors**: Weather, hunting pressure, and seasonal changes not directly considered
- **Sample Size**: Limited data reduces prediction accuracy (addressed by threshold system)

## Usage Guidelines

### Best Practices
1. **Adequate Data**: Ensure minimum 10+ sightings and 3+ transitions for reliable predictions
2. **Feature Mapping**: Accurately map and weight property features
3. **Seasonal Adjustments**: Update feature weights seasonally for improved accuracy
4. **Regular Updates**: Refresh predictions as new sightings are added
5. **Cross-Reference**: Compare predictions with actual observations for validation

### Interpretation
- **High Corridor Scores**: Indicate frequently used movement routes
- **Time Patterns**: Show when specific corridors are most active
- **Feature Weights**: Reflect relative importance in movement decisions
- **Limited Data Warnings**: Indicate need for more observation time

## Future Enhancements

### Potential Improvements
- **Environmental Integration**: Weather data correlation
- **Seasonal Migration**: Long-term movement pattern analysis
- **Behavior Classification**: Feeding, bedding, travel movement types
- **Machine Learning**: Advanced pattern recognition algorithms
- **Real-Time Updates**: Live prediction updates as new sightings arrive

## Testing

The enhanced BuckTrax system includes comprehensive test coverage:

### Test Categories
- **Corridor Inference**: Movement transition identification
- **Feature Weight Integration**: Scoring calculations with weights
- **Threshold Logic**: Limited data warning triggers
- **Time Segmentation**: Activity period filtering
- **Sighting Association**: Feature-camera proximity matching
- **Pattern Recognition**: Time-of-day movement patterns

### Quality Assurance
- All existing tests continue to pass
- New functionality covered by 8 additional test cases
- Performance benchmarks for large datasets
- User acceptance testing for UI enhancements

---

*This documentation covers the enhanced BuckTrax functionality implementing profile-based corridor movement prediction with feature weight integration and threshold-based data quality validation.*