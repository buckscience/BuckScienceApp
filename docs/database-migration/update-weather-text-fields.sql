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
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 0 AND 0.99 THEN 'N'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 1 AND 1.99 THEN 'NNE'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 2 AND 2.99 THEN 'NE'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 3 AND 3.99 THEN 'ENE'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 4 AND 4.99 THEN 'E'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 5 AND 5.99 THEN 'ESE'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 6 AND 6.99 THEN 'SE'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 7 AND 7.99 THEN 'SSE'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 8 AND 8.99 THEN 'S'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 9 AND 9.99 THEN 'SSW'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 10 AND 10.99 THEN 'SW'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 11 AND 11.99 THEN 'WSW'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 12 AND 12.99 THEN 'W'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 13 AND 13.99 THEN 'WNW'
                WHEN ((WindDirection % 360 + 360) % 360 + 11.25) / 22.5 % 16 BETWEEN 14 AND 14.99 THEN 'NW'
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