-- Database Migration Script: Populate Wind Direction and Moon Phase Text Fields
-- This script updates WindDirectionText and MoonPhaseText fields from their numeric counterparts
-- Run this script to populate missing text fields in existing weather data

-- Update WindDirectionText from WindDirection degrees using 16-point compass conversion
UPDATE Weather 
SET WindDirectionText = 
    CASE 
        WHEN WindDirection IS NULL THEN NULL
        ELSE 
            CASE 
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 0 THEN 'N'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 1 THEN 'NNE'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 2 THEN 'NE'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 3 THEN 'ENE'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 4 THEN 'E'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 5 THEN 'ESE'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 6 THEN 'SE'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 7 THEN 'SSE'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 8 THEN 'S'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 9 THEN 'SSW'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 10 THEN 'SW'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 11 THEN 'WSW'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 12 THEN 'W'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 13 THEN 'WNW'
                WHEN FLOOR(((WindDirection % 360.0 + 360.0) % 360.0 + 11.25) / 22.5) % 16.0 = 14 THEN 'NW'
                ELSE 'NNW'
            END
    END
WHERE WindDirectionText IS NULL OR WindDirectionText = '';

-- Update MoonPhaseText from MoonPhase numeric values (0.0-1.0)
UPDATE Weather 
SET MoonPhaseText = 
    CASE 
        WHEN MoonPhase IS NULL THEN NULL
        WHEN MoonPhase >= 0.0 AND MoonPhase < 0.05 THEN 'New Moon'
        WHEN MoonPhase >= 0.05 AND MoonPhase < 0.2 THEN 'Waxing Crescent'
        WHEN MoonPhase >= 0.2 AND MoonPhase < 0.3 THEN 'First Quarter'
        WHEN MoonPhase >= 0.3 AND MoonPhase < 0.45 THEN 'Waxing Gibbous'
        WHEN MoonPhase >= 0.45 AND MoonPhase < 0.55 THEN 'Full Moon'
        WHEN MoonPhase >= 0.55 AND MoonPhase < 0.7 THEN 'Waning Gibbous'
        WHEN MoonPhase >= 0.7 AND MoonPhase < 0.8 THEN 'Last Quarter'
        WHEN MoonPhase >= 0.8 AND MoonPhase < 0.95 THEN 'Waning Crescent'
        ELSE 'New Moon'
    END
WHERE MoonPhaseText IS NULL OR MoonPhaseText = '';

-- Verification queries to check the results
SELECT 'Wind Direction Conversion Results' as Report;
SELECT 
    WindDirection,
    WindDirectionText,
    COUNT(*) as Count
FROM Weather 
WHERE WindDirection IS NOT NULL
GROUP BY WindDirection, WindDirectionText
ORDER BY WindDirection;

SELECT 'Moon Phase Conversion Results' as Report;
SELECT 
    MoonPhase,
    MoonPhaseText,
    COUNT(*) as Count
FROM Weather 
WHERE MoonPhase IS NOT NULL
GROUP BY MoonPhase, MoonPhaseText
ORDER BY MoonPhase;

-- Summary of updates
SELECT 
    'Weather Records Updated' as Report,
    SUM(CASE WHEN WindDirectionText IS NOT NULL AND WindDirection IS NOT NULL THEN 1 ELSE 0 END) as WindDirectionUpdates,
    SUM(CASE WHEN MoonPhaseText IS NOT NULL AND MoonPhase IS NOT NULL THEN 1 ELSE 0 END) as MoonPhaseUpdates,
    COUNT(*) as TotalWeatherRecords
FROM Weather;