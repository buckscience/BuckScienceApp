using System;

namespace BuckScience.Domain.Entities
{
    public class Weather
    {
        protected Weather() { } // EF

        public Weather(
            DateTime dateTime,
            int dateTimeEpoch,
            double temperature,
            double windSpeed,
            double windDirection,
            string? windDirectionText,
            double visibility,
            double pressure,
            string? pressureTrend,
            double humidity,
            string? conditions,
            string? icon,
            int sunriseEpoch,
            int sunsetEpoch,
            double cloudCover,
            double moonPhase,
            string? moonPhaseText)
        {
            DateTime = dateTime;
            DateTimeEpoch = dateTimeEpoch;
            Temperature = temperature;
            WindSpeed = windSpeed;
            WindDirection = windDirection;
            WindDirectionText = (windDirectionText ?? string.Empty).Trim();
            Visibility = visibility;
            Pressure = pressure;
            PressureTrend = (pressureTrend ?? string.Empty).Trim();
            Humidity = humidity;
            Conditions = (conditions ?? string.Empty).Trim();
            Icon = (icon ?? string.Empty).Trim();
            SunriseEpoch = sunriseEpoch;
            SunsetEpoch = sunsetEpoch;
            CloudCover = cloudCover;
            MoonPhase = moonPhase;
            MoonPhaseText = (moonPhaseText ?? string.Empty).Trim();
        }

        public int Id { get; private set; }

        public DateTime DateTime { get; private set; }
        public int DateTimeEpoch { get; private set; }

        public double Temperature { get; private set; }
        public double WindSpeed { get; private set; }
        public double WindDirection { get; private set; }
        public string WindDirectionText { get; private set; } = string.Empty;

        public double Visibility { get; private set; }
        public double Pressure { get; private set; }
        public string PressureTrend { get; private set; } = string.Empty;

        public double Humidity { get; private set; }
        public string Conditions { get; private set; } = string.Empty;
        public string Icon { get; private set; } = string.Empty;

        public int SunriseEpoch { get; private set; }
        public int SunsetEpoch { get; private set; }

        public double CloudCover { get; private set; }
        public double MoonPhase { get; private set; }
        public string MoonPhaseText { get; private set; } = string.Empty;

        public void Update(
            double temperature,
            double windSpeed,
            double windDirection,
            string? windDirectionText,
            double visibility,
            double pressure,
            string? pressureTrend,
            double humidity,
            string? conditions,
            string? icon,
            int sunriseEpoch,
            int sunsetEpoch,
            double cloudCover,
            double moonPhase,
            string? moonPhaseText)
        {
            Temperature = temperature;
            WindSpeed = windSpeed;
            WindDirection = windDirection;
            WindDirectionText = (windDirectionText ?? string.Empty).Trim();
            Visibility = visibility;
            Pressure = pressure;
            PressureTrend = (pressureTrend ?? string.Empty).Trim();
            Humidity = humidity;
            Conditions = (conditions ?? string.Empty).Trim();
            Icon = (icon ?? string.Empty).Trim();
            SunriseEpoch = sunriseEpoch;
            SunsetEpoch = sunsetEpoch;
            CloudCover = cloudCover;
            MoonPhase = moonPhase;
            MoonPhaseText = (moonPhaseText ?? string.Empty).Trim();
        }
    }
}