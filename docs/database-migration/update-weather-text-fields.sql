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
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 0 THEN 'N'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 1 THEN 'NNE'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 2 THEN 'NE'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 3 THEN 'ENE'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 4 THEN 'E'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 5 THEN 'ESE'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 6 THEN 'SE'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 7 THEN 'SSE'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 8 THEN 'S'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 9 THEN 'SSW'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 10 THEN 'SW'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 11 THEN 'WSW'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 12 THEN 'W'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 13 THEN 'WNW'
                WHEN FLOOR(((WindDirection % CAST(360 AS FLOAT) + CAST(360 AS FLOAT)) % CAST(360 AS FLOAT) + CAST(11.25 AS FLOAT)) / CAST(22.5 AS FLOAT)) % CAST(16 AS FLOAT) = 14 THEN 'NW'
                ELSE 'NNW'
            END
    END
WHERE WindDirectionText IS NULL OR WindDirectionText = '';

-- Update MoonPhaseText from MoonPhase numeric values (0.0-1.0)
UPDATE Weather 
SET MoonPhaseText = 
    CASE 
        WHEN MoonPhase IS NULL THEN NULL
        WHEN MoonPhase >= CAST(0.0 AS FLOAT) AND MoonPhase < CAST(0.05 AS FLOAT) THEN 'New Moon'
        WHEN MoonPhase >= CAST(0.05 AS FLOAT) AND MoonPhase < CAST(0.2 AS FLOAT) THEN 'Waxing Crescent'
        WHEN MoonPhase >= CAST(0.2 AS FLOAT) AND MoonPhase < CAST(0.3 AS FLOAT) THEN 'First Quarter'
        WHEN MoonPhase >= CAST(0.3 AS FLOAT) AND MoonPhase < CAST(0.45 AS FLOAT) THEN 'Waxing Gibbous'
        WHEN MoonPhase >= CAST(0.45 AS FLOAT) AND MoonPhase < CAST(0.55 AS FLOAT) THEN 'Full Moon'
        WHEN MoonPhase >= CAST(0.55 AS FLOAT) AND MoonPhase < CAST(0.7 AS FLOAT) THEN 'Waning Gibbous'
        WHEN MoonPhase >= CAST(0.7 AS FLOAT) AND MoonPhase < CAST(0.8 AS FLOAT) THEN 'Last Quarter'
        WHEN MoonPhase >= CAST(0.8 AS FLOAT) AND MoonPhase < CAST(0.95 AS FLOAT) THEN 'Waning Crescent'
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