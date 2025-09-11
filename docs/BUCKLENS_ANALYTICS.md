# BuckLens Analytics Module

## Overview

The BuckLens Analytics module provides comprehensive data visualization and analysis for animal profile sightings combined with weather data. This module extends the Profile Details view with interactive charts, summary statistics, and data export capabilities.

## Features

### Analytics Components

1. **Summary Analysis**
   - Total sightings count from tagged photos
   - Date range of sightings
   - "Best odds" analysis identifying optimal hunting conditions

2. **Interactive Charts**
   - **Sightings by Camera**: Bar chart showing sighting frequency per camera location
   - **Sightings by Time of Day**: Pie chart with Morning/Midday/Evening/Night segments
   - **Sightings by Moon Phase**: Bar chart with moon phase icons and descriptions
   - **Sightings by Wind Direction**: Radar chart showing wind direction patterns
   - **Sightings by Temperature**: Histogram showing temperature distribution in 5°F bins
   - **Sighting Locations**: Enhanced location cards with weather metadata

3. **Data Export**
   - CSV export for spreadsheet analysis
   - JSON export for programmatic use

### API Endpoints

All endpoints require authentication and verify profile ownership.

#### Summary Data
- `GET /profiles/{id}/analytics/summary`
  - Returns aggregated analytics data and best odds analysis

#### Chart Data
- `GET /profiles/{id}/analytics/charts/cameras`
- `GET /profiles/{id}/analytics/charts/timeofday`
- `GET /profiles/{id}/analytics/charts/moonphase`
- `GET /profiles/{id}/analytics/charts/winddirection`
- `GET /profiles/{id}/analytics/charts/temperature`

#### Location Data
- `GET /profiles/{id}/analytics/sightings/locations`
  - Returns sighting coordinates for heatmap visualization

## Technical Implementation

### Backend Components

#### BuckLensAnalyticsService
Location: `src/BuckScience.Application/Analytics/BuckLensAnalyticsService.cs`

Core service responsible for:
- Querying sighting data with associated weather information
- Aggregating data for various chart types
- Performing best odds analysis
- Handling camera placement history for accurate location names

#### ProfilesController Extensions
Location: `src/BuckScience.Web/Controllers/ProfilesController.cs`

Added API endpoints for serving chart data and analytics summaries.

### Frontend Components

#### BuckLens Charts JavaScript Module
Location: `src/BuckScience.Web/wwwroot/js/bucklens-charts.js`

Features:
- Chart.js integration for all visualization types
- Responsive design for mobile and desktop
- Interactive elements with hover effects
- Data export functionality
- Loading states and error handling

#### CSS Styling
Location: `src/BuckScience.Web/wwwroot/css/site.css`

Custom styles for:
- Chart containers with hover effects
- Moon phase and wind direction icons
- Temperature color gradients
- Responsive layout adjustments

### Data Models

#### SightingData
Represents a single sighting event with:
- Photo metadata (ID, date taken)
- Camera information (ID, name, location)
- Weather data (temperature, wind, moon phase, etc.)

#### ChartData
Standardized format for chart visualization:
- Chart type specification
- Data points with labels and values
- Metadata for drilldown functionality

#### BestOddsAnalysis
Analysis results including:
- Summary text generation
- Best time of day
- Best camera location
- Optimal weather conditions

## Usage

### Viewing Analytics

1. Navigate to any Profile Details page
2. Expand the "BuckLens Analytics" accordion section
3. Charts load automatically with the profile's sighting data
4. Interact with charts for detailed information

### Data Export

1. Scroll to the bottom of the BuckLens Analytics section
2. Click "Export CSV" or "Export JSON"
3. File downloads automatically with profile-specific filename

### Best Odds Analysis

The system automatically analyzes patterns in sighting data to provide insights such as:
- "Best odds for a sighting are during morning at the Crossroads camera when the wind is from the North during a Waxing Crescent moon"

## Configuration

### Time of Day Segments

Currently configured as:
- Morning: 5:00 AM - 10:00 AM
- Midday: 10:00 AM - 3:00 PM
- Evening: 3:00 PM - 8:00 PM
- Night: 8:00 PM - 5:00 AM

### Temperature Binning

Temperature data is grouped into 5°F increments for histogram visualization.

### Color Scheme

Uses the application's primary color palette:
- Primary: #527A52 (Forest Green)
- Secondary: #8CAF8C (Light Green)
- Accent: #4a5a5f (Dark Gray)

## Future Enhancements

### Planned Features

1. **Date Range Filtering**
   - Allow users to filter analytics by specific date ranges
   - Seasonal comparison capabilities

2. **Map Integration**
   - Integrate location heatmap with existing map system
   - Show sighting density overlays
   - Click to view camera details on map

3. **Advanced Drilldown**
   - Click chart elements to view detailed sighting lists
   - Modal windows with photo previews
   - Filter integration between charts

4. **Weather Pattern Analysis**
   - Barometric pressure trend correlation
   - Weather front analysis
   - Historical weather comparison

5. **Predictive Analytics**
   - Machine learning models for optimal hunting times
   - Weather forecast integration
   - Seasonal migration pattern prediction

## Performance Considerations

- Analytics queries include conditional joins for weather data
- Chart rendering is optimized for mobile devices
- Data is cached at the profile level for performance
- Lazy loading of chart components reduces initial page load

## Security

- All endpoints verify user ownership of profiles
- Data is scoped to user's properties only
- No sensitive location data exposed in client-side code
- Export functionality respects user permissions