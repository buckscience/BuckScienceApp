using System;
using System.Collections.Generic;

namespace BuckScience.Application.Photos;

public class PhotoFilters
{
    // Date/Time filters
    public DateTime? DateTakenFrom { get; set; }
    public DateTime? DateTakenTo { get; set; }
    public DateTime? DateUploadedFrom { get; set; }
    public DateTime? DateUploadedTo { get; set; }
    
    // Camera filters
    public List<int>? CameraIds { get; set; }
    
    // Weather-related filters
    public double? TemperatureMin { get; set; }
    public double? TemperatureMax { get; set; }
    
    public double? WindSpeedMin { get; set; }
    public double? WindSpeedMax { get; set; }
    
    public double? HumidityMin { get; set; }
    public double? HumidityMax { get; set; }
    
    public double? PressureMin { get; set; }
    public double? PressureMax { get; set; }
    
    public double? VisibilityMin { get; set; }
    public double? VisibilityMax { get; set; }
    
    public double? CloudCoverMin { get; set; }
    public double? CloudCoverMax { get; set; }
    
    public double? MoonPhaseMin { get; set; }
    public double? MoonPhaseMax { get; set; }
    
    public List<string>? Conditions { get; set; }
    public List<string>? MoonPhaseTexts { get; set; }
    public List<string>? PressureTrends { get; set; }
    public List<string>? WindDirectionTexts { get; set; }
    
    // Helper method to check if any weather filters are applied
    public bool HasWeatherFilters => 
        TemperatureMin.HasValue || TemperatureMax.HasValue ||
        WindSpeedMin.HasValue || WindSpeedMax.HasValue ||
        HumidityMin.HasValue || HumidityMax.HasValue ||
        PressureMin.HasValue || PressureMax.HasValue ||
        VisibilityMin.HasValue || VisibilityMax.HasValue ||
        CloudCoverMin.HasValue || CloudCoverMax.HasValue ||
        MoonPhaseMin.HasValue || MoonPhaseMax.HasValue ||
        (Conditions?.Count > 0) ||
        (MoonPhaseTexts?.Count > 0) ||
        (PressureTrends?.Count > 0) ||
        (WindDirectionTexts?.Count > 0);
        
    // Helper method to check if any filters are applied
    public bool HasAnyFilters =>
        DateTakenFrom.HasValue || DateTakenTo.HasValue ||
        DateUploadedFrom.HasValue || DateUploadedTo.HasValue ||
        (CameraIds?.Count > 0) ||
        HasWeatherFilters;
}