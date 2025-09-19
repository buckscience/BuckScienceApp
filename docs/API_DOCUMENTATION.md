# BuckScience API Documentation

## 🌐 Overview

BuckScience provides both MVC web application endpoints and RESTful API endpoints for programmatic access to hunting analytics data. All endpoints require authentication and respect user ownership boundaries.

### Base URL
- **Development**: `https://localhost:5001`
- **Production**: `https://yourdomain.com`

### Authentication
All API endpoints require user authentication via Azure AD B2C. Include authentication headers or use cookie-based authentication from web application.

---

## 🦌 BuckTrax API

### Movement Prediction

#### Generate Movement Predictions
```http
POST /bucktrax/api/predict
Content-Type: application/json
Authorization: Bearer {token}

{
    "profileId": 123,
    "season": "Fall",          // Optional: "EarlySeason", "PreRut", "Rut", "PostRut", "LateSeasonPost", "YearRound"
    "timeOfDayFilter": 8       // Optional: Hour filter (0-23)
}
```

**Response (200 OK)**:
```json
{
    "profileId": 123,
    "profileName": "Big Buck",
    "propertyName": "North Property",
    "totalSightings": 45,
    "totalTransitions": 12,
    "predictionDate": "2024-01-15T10:30:00Z",
    "isLimitedData": false,
    "limitedDataMessage": null,
    "timeSegments": [
        {
            "timeSegment": "Early",
            "startHour": 6,
            "endHour": 11,
            "sightingCount": 15,
            "confidenceScore": 85.2,
            "predictedZones": [
                {
                    "locationName": "Oak Ridge Food Plot",
                    "latitude": 40.12345,
                    "longitude": -74.67890,
                    "probability": 0.85,
                    "sightingCount": 8,
                    "radiusMeters": 150,
                    "isCorridorPrediction": false,
                    "associatedFeatureId": 25,
                    "featureType": "Food Plot",
                    "featureWeight": 0.9
                }
            ],
            "timeSegmentCorridors": [
                {
                    "startFeatureName": "Creek Crossing North",
                    "endFeatureName": "Oak Ridge Food Plot", 
                    "startLatitude": 40.12000,
                    "startLongitude": -74.67500,
                    "endLatitude": 40.12345,
                    "endLongitude": -74.67890,
                    "transitionCount": 5,
                    "corridorScore": 23.5,
                    "distance": 245.7,
                    "averageTimeSpan": 25.4,
                    "timeOfDayPattern": "Morning",
                    "routeId": null,
                    "routePoints": [],
                    "isPartOfMultiPointRoute": false
                }
            ]
        }
    ],
    "movementCorridors": [
        {
            "startFeatureName": "Creek Crossing North",
            "endFeatureName": "Oak Ridge Food Plot",
            "startLatitude": 40.12000,
            "startLongitude": -74.67500,
            "endLatitude": 40.12345,
            "endLongitude": -74.67890,
            "transitionCount": 8,
            "corridorScore": 45.2,
            "distance": 245.7,
            "averageTimeSpan": 28.5,
            "timeOfDayPattern": "Morning, Evening",
            "routeId": "route-1",
            "routePoints": [
                {
                    "order": 1,
                    "locationId": 15,
                    "locationName": "Creek Crossing North",
                    "locationType": "Feature",
                    "latitude": 40.12000,
                    "longitude": -74.67500,
                    "visitTime": "2024-01-10T07:15:00Z"
                },
                {
                    "order": 2,
                    "locationId": 25,
                    "locationName": "Oak Ridge Food Plot",
                    "locationType": "Feature", 
                    "latitude": 40.12345,
                    "longitude": -74.67890,
                    "visitTime": "2024-01-10T07:45:00Z"
                }
            ],
            "isPartOfMultiPointRoute": false
        }
    ],
    "configuration": {
        "movementTimeWindowMinutes": 480,
        "maxMovementDistanceMeters": 5000,
        "cameraFeatureProximityMeters": 100,
        "minimumSightingsThreshold": 10,
        "minimumTransitionsThreshold": 3,
        "showLimitedDataWarning": true,
        "enableFeatureAwareRouting": true,
        "minimumDistanceForFeatureRouting": 200,
        "maximumDetourPercentage": 0.3,
        "maximumWaypointsPerRoute": 2
    },
    "defaultTimeSegmentIndex": 0
}
```

**Error Responses**:
- `401 Unauthorized`: User not authenticated
- `403 Forbidden`: User doesn't own the specified profile
- `404 Not Found`: Profile not found
- `400 Bad Request`: Invalid request parameters

#### Get Property Profiles
```http
GET /bucktrax/api/properties/{propertyId}/profiles
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
    {
        "id": 123,
        "name": "Big Buck",
        "profileStatus": 1,  // 1 = Active, 0 = Inactive
        "tagName": "Buck001",
        "coverPhotoUrl": "https://storage.blob.core.windows.net/photos/cover123.jpg"
    },
    {
        "id": 124,
        "name": "Young Buck",
        "profileStatus": 1,
        "tagName": "Buck002", 
        "coverPhotoUrl": null
    }
]
```

#### Get Configuration
```http
GET /bucktrax/api/configuration
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "movementTimeWindowMinutes": 480,
    "maxMovementDistanceMeters": 5000,
    "cameraFeatureProximityMeters": 100,
    "minimumSightingsThreshold": 10,
    "minimumTransitionsThreshold": 3,
    "showLimitedDataWarning": true,
    "enableFeatureAwareRouting": true,
    "minimumDistanceForFeatureRouting": 200,
    "maximumDetourPercentage": 0.3,
    "maximumWaypointsPerRoute": 2
}
```

---

## 📊 BuckLens Analytics API

### Profile Analytics

#### Get Analytics Summary
```http
GET /profiles/{profileId}/analytics/summary
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "profileId": 123,
    "totalSightings": 45,
    "dateRange": {
        "startDate": "2023-10-01",
        "endDate": "2024-01-15"
    },
    "bestOdds": {
        "summary": "Best odds for a sighting are during morning at the Oak Ridge camera when the wind is from the North during a Waxing Crescent moon",
        "bestTimeOfDay": "Morning",
        "bestCamera": "Oak Ridge",
        "bestConditions": {
            "windDirection": "North",
            "moonPhase": "Waxing Crescent",
            "temperatureRange": "35-45°F"
        }
    },
    "activityLevel": "High",
    "confidence": 87.5
}
```

#### Get Chart Data

##### Sightings by Camera
```http
GET /profiles/{profileId}/analytics/charts/cameras
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "chartType": "bar",
    "dataPoints": [
        {
            "label": "Oak Ridge",
            "value": 18,
            "metadata": {
                "cameraId": 1,
                "coordinates": [40.12345, -74.67890],
                "lastSighting": "2024-01-15T08:30:00Z"
            }
        },
        {
            "label": "Creek Crossing",
            "value": 12,
            "metadata": {
                "cameraId": 2,
                "coordinates": [40.12000, -74.67500],
                "lastSighting": "2024-01-14T17:15:00Z"
            }
        }
    ]
}
```

##### Sightings by Time of Day
```http
GET /profiles/{profileId}/analytics/charts/timeofday
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "chartType": "pie",
    "dataPoints": [
        {
            "label": "Morning (5-10 AM)",
            "value": 18,
            "metadata": {
                "timeSegment": "Morning",
                "startHour": 5,
                "endHour": 10,
                "percentage": 40.0
            }
        },
        {
            "label": "Midday (10 AM-3 PM)",
            "value": 5,
            "metadata": {
                "timeSegment": "Midday",
                "startHour": 10,
                "endHour": 15,
                "percentage": 11.1
            }
        },
        {
            "label": "Evening (3-8 PM)",
            "value": 15,
            "metadata": {
                "timeSegment": "Evening",
                "startHour": 15,
                "endHour": 20,
                "percentage": 33.3
            }
        },
        {
            "label": "Night (8 PM-5 AM)",
            "value": 7,
            "metadata": {
                "timeSegment": "Night",
                "startHour": 20,
                "endHour": 5,
                "percentage": 15.6
            }
        }
    ]
}
```

##### Sightings by Moon Phase
```http
GET /profiles/{profileId}/analytics/charts/moonphase
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "chartType": "bar",
    "dataPoints": [
        {
            "label": "New Moon",
            "value": 8,
            "metadata": {
                "moonPhase": "New Moon",
                "illumination": 0.0,
                "icon": "🌑"
            }
        },
        {
            "label": "Waxing Crescent",
            "value": 12,
            "metadata": {
                "moonPhase": "Waxing Crescent", 
                "illumination": 0.25,
                "icon": "🌒"
            }
        },
        {
            "label": "First Quarter",
            "value": 6,
            "metadata": {
                "moonPhase": "First Quarter",
                "illumination": 0.5,
                "icon": "🌓"
            }
        }
    ]
}
```

##### Sightings by Wind Direction
```http
GET /profiles/{profileId}/analytics/charts/winddirection
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "chartType": "radar",
    "dataPoints": [
        {
            "label": "North",
            "value": 12,
            "metadata": {
                "direction": "N",
                "degrees": 0,
                "averageSpeed": 8.5
            }
        },
        {
            "label": "Northeast", 
            "value": 8,
            "metadata": {
                "direction": "NE",
                "degrees": 45,
                "averageSpeed": 12.2
            }
        }
    ]
}
```

##### Sightings by Temperature
```http
GET /profiles/{profileId}/analytics/charts/temperature
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "chartType": "histogram",
    "dataPoints": [
        {
            "label": "30-35°F",
            "value": 5,
            "metadata": {
                "temperatureRange": "30-35",
                "binCenter": 32.5,
                "averageActivity": 0.6
            }
        },
        {
            "label": "35-40°F",
            "value": 12,
            "metadata": {
                "temperatureRange": "35-40",
                "binCenter": 37.5,
                "averageActivity": 0.8
            }
        }
    ]
}
```

#### Get Sighting Locations
```http
GET /profiles/{profileId}/analytics/sightings/locations
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "locations": [
        {
            "latitude": 40.12345,
            "longitude": -74.67890,
            "sightingCount": 18,
            "cameraName": "Oak Ridge",
            "lastSighting": "2024-01-15T08:30:00Z",
            "intensity": 0.9
        },
        {
            "latitude": 40.12000,
            "longitude": -74.67500,
            "sightingCount": 12,
            "cameraName": "Creek Crossing",
            "lastSighting": "2024-01-14T17:15:00Z", 
            "intensity": 0.6
        }
    ],
    "bounds": {
        "north": 40.12500,
        "south": 40.11500,
        "east": -74.67000,
        "west": -74.68000
    }
}
```

### Data Export

#### Export Profile Analytics (CSV)
```http
GET /profiles/{profileId}/analytics/export/csv
Authorization: Bearer {token}
```

**Response (200 OK)**:
```
Content-Type: text/csv
Content-Disposition: attachment; filename="big-buck-analytics-20240115.csv"

Date,Time,Camera,Temperature,WindDirection,WindSpeed,MoonPhase,MoonIllumination
2024-01-15,08:30,Oak Ridge,42,N,8.5,Waxing Crescent,0.35
2024-01-14,17:15,Creek Crossing,38,NE,12.2,Waxing Crescent,0.32
```

#### Export Profile Analytics (JSON)
```http
GET /profiles/{profileId}/analytics/export/json
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "profileName": "Big Buck",
    "exportDate": "2024-01-15T10:30:00Z",
    "dateRange": {
        "startDate": "2023-10-01",
        "endDate": "2024-01-15"
    },
    "sightings": [
        {
            "date": "2024-01-15",
            "time": "08:30",
            "camera": "Oak Ridge",
            "coordinates": [40.12345, -74.67890],
            "weather": {
                "temperature": 42,
                "windDirection": "N",
                "windSpeed": 8.5,
                "moonPhase": "Waxing Crescent",
                "moonIllumination": 0.35
            }
        }
    ],
    "summary": {
        "totalSightings": 45,
        "bestConditions": {
            "timeOfDay": "Morning",
            "camera": "Oak Ridge",
            "windDirection": "North"
        }
    }
}
```

---

## 🏡 Properties API

### Property Management

#### List Properties
```http
GET /properties
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
    {
        "id": 1,
        "name": "North Property",
        "latitude": 40.12345,
        "longitude": -74.67890,
        "acreage": 120.5,
        "cameraCount": 8,
        "photoCount": 1250,
        "profileCount": 3,
        "createdDate": "2023-08-15T09:00:00Z"
    }
]
```

#### Get Property Details
```http
GET /properties/{propertyId}
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "id": 1,
    "name": "North Property",
    "center": {
        "latitude": 40.12345,
        "longitude": -74.67890
    },
    "boundary": {
        "type": "Polygon",
        "coordinates": [[
            [-74.68000, 40.12000],
            [-74.67500, 40.12000], 
            [-74.67500, 40.12500],
            [-74.68000, 40.12500],
            [-74.68000, 40.12000]
        ]]
    },
    "timeZone": "America/New_York",
    "dayHour": 6,
    "nightHour": 20,
    "createdDate": "2023-08-15T09:00:00Z",
    "cameras": [
        {
            "id": 1,
            "name": "Oak Ridge",
            "latitude": 40.12345,
            "longitude": -74.67890,
            "isActive": true,
            "photoCount": 450
        }
    ],
    "features": [
        {
            "id": 25,
            "name": "Oak Ridge Food Plot",
            "classificationType": 2,
            "classificationName": "Food Plot",
            "geometryType": "Point",
            "coordinates": "POINT(-74.67890 40.12345)",
            "latitude": 40.12345,
            "longitude": -74.67890,
            "effectiveWeight": 0.9
        }
    ]
}
```

### Property Features

#### List Property Features
```http
GET /properties/{propertyId}/features
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
    {
        "id": 25,
        "name": "Oak Ridge Food Plot",
        "classificationType": 2,
        "classificationName": "Food Plot",
        "geometryType": "Point",
        "coordinates": "POINT(-74.67890 40.12345)",
        "latitude": 40.12345,
        "longitude": -74.67890,
        "effectiveWeight": 0.9,
        "notes": "Large clover plot, planted spring 2023",
        "createdAt": "2023-04-15T10:00:00Z"
    },
    {
        "id": 26,
        "name": "Creek Crossing North",
        "classificationType": 8,
        "classificationName": "Creek Crossing",
        "geometryType": "Point",
        "coordinates": "POINT(-74.67500 40.12000)",
        "latitude": 40.12000,
        "longitude": -74.67500,
        "effectiveWeight": 0.7,
        "notes": "Natural creek crossing, frequently used",
        "createdAt": "2023-04-15T10:15:00Z"
    }
]
```

#### Create Property Feature
```http
POST /properties/{propertyId}/features
Content-Type: application/json
Authorization: Bearer {token}

{
    "classificationType": 2,  // Food Plot
    "geometry": {
        "type": "Point",
        "coordinates": [-74.67890, 40.12345]
    },
    "name": "New Food Plot",
    "notes": "Planted with corn and soybeans",
    "weight": 0.8
}
```

**Response (201 Created)**:
```json
{
    "id": 27,
    "name": "New Food Plot",
    "classificationType": 2,
    "classificationName": "Food Plot",
    "geometryType": "Point",
    "coordinates": "POINT(-74.67890 40.12345)",
    "latitude": 40.12345,
    "longitude": -74.67890,
    "effectiveWeight": 0.8,
    "notes": "Planted with corn and soybeans",
    "createdAt": "2024-01-15T10:30:00Z"
}
```

### Feature Weights

#### Get Feature Weights
```http
GET /properties/{propertyId}/feature-weights
GET /properties/{propertyId}/feature-weights?season=Rut
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
    {
        "classificationType": 2,
        "classificationName": "Food Plot",
        "defaultWeight": 0.8,
        "userWeight": 0.9,
        "seasonalWeights": {
            "EarlySeason": 0.7,
            "PreRut": 0.8,
            "Rut": 0.6,
            "PostRut": 0.9,
            "LateSeasonPost": 0.9
        },
        "effectiveWeight": 0.6,
        "currentSeason": "Rut"
    },
    {
        "classificationType": 8,
        "classificationName": "Creek Crossing",
        "defaultWeight": 0.6,
        "userWeight": null,
        "seasonalWeights": null,
        "effectiveWeight": 0.6,
        "currentSeason": "Rut"
    }
]
```

#### Update Feature Weights
```http
PUT /properties/{propertyId}/feature-weights
Content-Type: application/json
Authorization: Bearer {token}

{
    "weights": [
        {
            "classificationType": 2,
            "userWeight": 0.95,
            "seasonalWeights": {
                "EarlySeason": 0.8,
                "PreRut": 0.9,
                "Rut": 0.7,
                "PostRut": 0.95,
                "LateSeasonPost": 0.95
            }
        }
    ]
}
```

**Response (200 OK)**:
```json
{
    "updated": true,
    "affectedWeights": 1
}
```

---

## 📷 Photos & Cameras API

### Photo Management

#### Upload Photos
```http
POST /properties/{propertyId}/photos/upload
Content-Type: multipart/form-data
Authorization: Bearer {token}

FormData:
- files: [photo1.jpg, photo2.jpg, ...]
- cameraId: 1
```

**Response (200 OK)**:
```json
{
    "uploadedCount": 25,
    "failedCount": 2,
    "processedCount": 23,
    "weatherLookupCount": 23,
    "results": [
        {
            "fileName": "IMG_001.jpg",
            "status": "Success",
            "photoId": 1001,
            "dateTaken": "2024-01-15T08:30:00Z",
            "hasGPS": true,
            "weatherAssigned": true
        },
        {
            "fileName": "IMG_002.jpg", 
            "status": "Failed",
            "error": "Invalid file format"
        }
    ]
}
```

#### List Photos
```http
GET /properties/{propertyId}/photos
GET /properties/{propertyId}/photos?cameraId=1&startDate=2024-01-01&endDate=2024-01-15
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "photos": [
        {
            "id": 1001,
            "filePath": "photos/property-1/camera-1/2024/01/IMG_001.jpg",
            "dateTaken": "2024-01-15T08:30:00Z",
            "cameraId": 1,
            "cameraName": "Oak Ridge",
            "latitude": 40.12345,
            "longitude": -74.67890,
            "hasGPS": true,
            "weather": {
                "temperature": 42,
                "windDirection": "N",
                "windSpeed": 8.5,
                "moonPhase": "Waxing Crescent"
            },
            "tags": ["Buck001", "MainAntlerPoints"]
        }
    ],
    "pagination": {
        "page": 1,
        "pageSize": 50,
        "totalCount": 1250,
        "totalPages": 25
    }
}
```

### Camera Management

#### List Cameras
```http
GET /properties/{propertyId}/cameras
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
    {
        "id": 1,
        "name": "Oak Ridge",
        "latitude": 40.12345,
        "longitude": -74.67890,
        "directionDegrees": 180,
        "directionText": "South",
        "isActive": true,
        "deploymentDate": "2023-09-01T00:00:00Z",
        "lastPhotoDate": "2024-01-15T08:30:00Z",
        "photoCount": 450,
        "placementHistory": [
            {
                "id": 101,
                "startDate": "2023-09-01T00:00:00Z",
                "endDate": null,
                "latitude": 40.12345,
                "longitude": -74.67890,
                "notes": "Overlooking food plot"
            }
        ]
    }
]
```

#### Create Camera
```http
POST /properties/{propertyId}/cameras
Content-Type: application/json
Authorization: Bearer {token}

{
    "name": "New Camera Location",
    "latitude": 40.12500,
    "longitude": -74.67600,
    "directionDegrees": 90,
    "notes": "Overlooking creek crossing"
}
```

**Response (201 Created)**:
```json
{
    "id": 9,
    "name": "New Camera Location",
    "latitude": 40.12500,
    "longitude": -74.67600,
    "directionDegrees": 90,
    "directionText": "East",
    "isActive": true,
    "deploymentDate": "2024-01-15T10:30:00Z",
    "photoCount": 0
}
```

---

## 🏷️ Tags & Profiles API

### Profile Management

#### List Profiles
```http
GET /properties/{propertyId}/profiles
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
    {
        "id": 123,
        "name": "Big Buck",
        "profileStatus": 1,
        "tagName": "Buck001",
        "coverPhotoUrl": "https://storage.blob.core.windows.net/photos/cover123.jpg",
        "photoCount": 45,
        "lastSighting": "2024-01-15T08:30:00Z",
        "property": {
            "id": 1,
            "name": "North Property"
        }
    }
]
```

#### Create Profile
```http
POST /properties/{propertyId}/profiles
Content-Type: application/json
Authorization: Bearer {token}

{
    "name": "New Buck Profile",
    "tagId": 5,
    "profileStatus": 1  // 1 = Active
}
```

**Response (201 Created)**:
```json
{
    "id": 125,
    "name": "New Buck Profile",
    "profileStatus": 1,
    "tagId": 5,
    "tagName": "Buck005",
    "coverPhotoUrl": null,
    "photoCount": 0
}
```

### Tag Management

#### List Tags
```http
GET /properties/{propertyId}/tags
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
[
    {
        "id": 1,
        "tagName": "Buck001",
        "isSystemTag": false,
        "photoCount": 45,
        "profileCount": 1,
        "lastUsed": "2024-01-15T08:30:00Z"
    },
    {
        "id": 10,
        "tagName": "MainAntlerPoints",
        "isSystemTag": true,
        "photoCount": 125,
        "profileCount": 0,
        "lastUsed": "2024-01-15T09:15:00Z"
    }
]
```

---

## 🌤️ Weather API

### Weather Data

#### Get Weather for Location/Date
```http
GET /weather/lookup
GET /weather/lookup?latitude=40.12345&longitude=-74.67890&date=2024-01-15
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "date": "2024-01-15",
    "latitude": 40.12345,
    "longitude": -74.67890,
    "hourlyData": [
        {
            "hour": 8,
            "temperature": 42,
            "humidity": 65,
            "windDirection": "N",
            "windSpeed": 8.5,
            "windGust": 12.0,
            "visibility": 10.0,
            "uvIndex": 2,
            "conditions": "Partly Cloudy",
            "precipitationProbability": 10,
            "precipitationIntensity": 0.0,
            "precipitationType": null,
            "pressure": 30.15,
            "moonPhase": "Waxing Crescent",
            "moonIllumination": 0.35
        }
    ]
}
```

---

## 📋 Subscription API

### Subscription Management

#### Get User Subscription
```http
GET /subscription/status
Authorization: Bearer {token}
```

**Response (200 OK)**:
```json
{
    "tier": "Pro",
    "status": "Active",
    "expirationDate": "2024-12-15T00:00:00Z",
    "trialDaysRemaining": 0,
    "isTrialExpired": false,
    "usage": {
        "photos": {
            "current": 2500,
            "limit": 10000,
            "percentage": 25.0
        },
        "cameras": {
            "current": 8,
            "limit": 25,
            "percentage": 32.0
        },
        "properties": {
            "current": 2,
            "limit": 10,
            "percentage": 20.0
        }
    },
    "features": {
        "buckTrax": true,
        "buckLens": true,
        "weatherIntegration": true,
        "advancedAnalytics": true,
        "apiAccess": true
    }
}
```

---

## 🔧 Common Response Patterns

### Standard Error Response
```json
{
    "error": {
        "code": "RESOURCE_NOT_FOUND",
        "message": "The requested profile was not found",
        "details": {
            "resource": "Profile",
            "resourceId": 123
        }
    },
    "timestamp": "2024-01-15T10:30:00Z",
    "path": "/bucktrax/api/predict"
}
```

### Pagination Response
```json
{
    "data": [...],
    "pagination": {
        "page": 1,
        "pageSize": 50,
        "totalCount": 1250,
        "totalPages": 25,
        "hasNextPage": true,
        "hasPreviousPage": false
    }
}
```

### Validation Error Response
```json
{
    "error": {
        "code": "VALIDATION_FAILED",
        "message": "Request validation failed",
        "validationErrors": [
            {
                "field": "profileId",
                "message": "ProfileId is required"
            },
            {
                "field": "timeOfDayFilter",
                "message": "TimeOfDayFilter must be between 0 and 23"
            }
        ]
    }
}
```

---

## 🛡️ Rate Limiting

### Rate Limits
- **Standard Endpoints**: 100 requests per minute per user
- **Analytics Endpoints**: 30 requests per minute per user  
- **BuckTrax Predictions**: 10 requests per minute per user
- **Photo Upload**: 50 MB per minute per user

### Rate Limit Headers
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 87
X-RateLimit-Reset: 1642681200
```

---

## 📈 Monitoring & Health

### Health Check
```http
GET /health
```

**Response (200 OK)**:
```json
{
    "status": "Healthy",
    "timestamp": "2024-01-15T10:30:00Z",
    "version": "2.1.0",
    "components": {
        "database": "Healthy",
        "storage": "Healthy", 
        "weatherApi": "Healthy"
    }
}
```

---

*This API documentation provides comprehensive coverage of all BuckScience endpoints. For implementation examples and client libraries, refer to the developer guide and code samples in the repository.*