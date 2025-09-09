using BuckScience.Domain.Enums;

namespace BuckScience.Domain.Helpers;

public static class FeatureWeightHelper
{
    public static float GetDefaultWeight(ClassificationType classificationType)
    {
        return classificationType switch
        {
            // Food sources - high importance
            ClassificationType.AgCropField => 0.8f,
            ClassificationType.FoodPlot => 0.8f,
            ClassificationType.MastTreePatch => 0.7f,
            ClassificationType.BrowsePatch => 0.6f,
            ClassificationType.PrairieForbPatch => 0.5f,

            // Water sources - important
            ClassificationType.Creek => 0.6f,
            ClassificationType.Pond => 0.7f,
            ClassificationType.Lake => 0.6f,
            ClassificationType.Spring => 0.7f,
            ClassificationType.Waterhole => 0.8f,
            ClassificationType.Trough => 0.6f,

            // Bedding and cover - critical
            ClassificationType.BeddingArea => 0.9f,
            ClassificationType.ThickBrush => 0.7f,
            ClassificationType.Clearcut => 0.5f,
            ClassificationType.CRP => 0.6f,
            ClassificationType.Swamp => 0.6f,
            ClassificationType.CedarThicket => 0.7f,
            ClassificationType.LeewardSlope => 0.6f,
            ClassificationType.EdgeCover => 0.7f,
            ClassificationType.IsolatedCover => 0.6f,
            ClassificationType.ManMadeCover => 0.4f,

            // Topographical features - moderate to high importance for movement
            ClassificationType.Ridge => 0.6f,
            ClassificationType.RidgePoint => 0.7f,
            ClassificationType.RidgeSpur => 0.6f,
            ClassificationType.Saddle => 0.8f,
            ClassificationType.Bench => 0.6f,
            ClassificationType.Draw => 0.7f,
            ClassificationType.CreekCrossing => 0.8f,
            ClassificationType.Ditch => 0.4f,
            ClassificationType.Valley => 0.5f,
            ClassificationType.Bluff => 0.5f,
            ClassificationType.FieldEdge => 0.7f,
            ClassificationType.InsideCorner => 0.8f,
            ClassificationType.Peninsula => 0.6f,
            ClassificationType.Island => 0.5f,
            ClassificationType.PinchPointFunnel => 0.9f,
            ClassificationType.TravelCorridor => 0.8f,
            ClassificationType.Spur => 0.6f,
            ClassificationType.Knob => 0.5f,

            // Default
            _ => 0.5f
        };
    }

    public static string GetDisplayName(ClassificationType classificationType)
    {
        return classificationType switch
        {
            ClassificationType.AgCropField => "Agricultural Crop Field",
            ClassificationType.FoodPlot => "Food Plot",
            ClassificationType.MastTreePatch => "Mast Tree Patch",
            ClassificationType.BrowsePatch => "Browse Patch",
            ClassificationType.PrairieForbPatch => "Prairie Forb Patch",
            ClassificationType.Creek => "Creek",
            ClassificationType.Pond => "Pond",
            ClassificationType.Lake => "Lake",
            ClassificationType.Spring => "Spring",
            ClassificationType.Waterhole => "Waterhole",
            ClassificationType.Trough => "Trough",
            ClassificationType.BeddingArea => "Bedding Area",
            ClassificationType.ThickBrush => "Thick Brush",
            ClassificationType.Clearcut => "Clearcut",
            ClassificationType.CRP => "CRP (Conservation Reserve Program)",
            ClassificationType.Swamp => "Swamp",
            ClassificationType.CedarThicket => "Cedar Thicket",
            ClassificationType.LeewardSlope => "Leeward Slope",
            ClassificationType.EdgeCover => "Edge Cover",
            ClassificationType.IsolatedCover => "Isolated Cover",
            ClassificationType.ManMadeCover => "Man-made Cover",
            ClassificationType.Ridge => "Ridge",
            ClassificationType.RidgePoint => "Ridge Point",
            ClassificationType.RidgeSpur => "Ridge Spur",
            ClassificationType.Saddle => "Saddle",
            ClassificationType.Bench => "Bench",
            ClassificationType.Draw => "Draw",
            ClassificationType.CreekCrossing => "Creek Crossing",
            ClassificationType.Ditch => "Ditch",
            ClassificationType.Valley => "Valley",
            ClassificationType.Bluff => "Bluff",
            ClassificationType.FieldEdge => "Field Edge",
            ClassificationType.InsideCorner => "Inside Corner",
            ClassificationType.Peninsula => "Peninsula",
            ClassificationType.Island => "Island",
            ClassificationType.PinchPointFunnel => "Pinch Point/Funnel",
            ClassificationType.TravelCorridor => "Travel Corridor",
            ClassificationType.Spur => "Spur",
            ClassificationType.Knob => "Knob",
            _ => classificationType.ToString()
        };
    }

    public static FeatureCategory GetCategory(ClassificationType classificationType)
    {
        return classificationType switch
        {
            // Food resources
            ClassificationType.AgCropField => FeatureCategory.ResourceFood,
            ClassificationType.FoodPlot => FeatureCategory.ResourceFood,
            ClassificationType.MastTreePatch => FeatureCategory.ResourceFood,
            ClassificationType.BrowsePatch => FeatureCategory.ResourceFood,
            ClassificationType.PrairieForbPatch => FeatureCategory.ResourceFood,

            // Water resources
            ClassificationType.Creek => FeatureCategory.ResourceWater,
            ClassificationType.Pond => FeatureCategory.ResourceWater,
            ClassificationType.Lake => FeatureCategory.ResourceWater,
            ClassificationType.Spring => FeatureCategory.ResourceWater,
            ClassificationType.Waterhole => FeatureCategory.ResourceWater,
            ClassificationType.Trough => FeatureCategory.ResourceWater,

            // Bedding resources
            ClassificationType.BeddingArea => FeatureCategory.ResourceBedding,
            ClassificationType.ThickBrush => FeatureCategory.ResourceBedding,
            ClassificationType.Clearcut => FeatureCategory.ResourceBedding,
            ClassificationType.CRP => FeatureCategory.ResourceBedding,
            ClassificationType.Swamp => FeatureCategory.ResourceBedding,
            ClassificationType.CedarThicket => FeatureCategory.ResourceBedding,
            ClassificationType.LeewardSlope => FeatureCategory.ResourceBedding,
            ClassificationType.EdgeCover => FeatureCategory.ResourceBedding,
            ClassificationType.IsolatedCover => FeatureCategory.ResourceBedding,
            ClassificationType.ManMadeCover => FeatureCategory.ResourceBedding,

            // Topographical features
            ClassificationType.Ridge => FeatureCategory.Topographical,
            ClassificationType.RidgePoint => FeatureCategory.Topographical,
            ClassificationType.RidgeSpur => FeatureCategory.Topographical,
            ClassificationType.Saddle => FeatureCategory.Topographical,
            ClassificationType.Bench => FeatureCategory.Topographical,
            ClassificationType.Draw => FeatureCategory.Topographical,
            ClassificationType.CreekCrossing => FeatureCategory.Topographical,
            ClassificationType.Ditch => FeatureCategory.Topographical,
            ClassificationType.Valley => FeatureCategory.Topographical,
            ClassificationType.Bluff => FeatureCategory.Topographical,
            ClassificationType.FieldEdge => FeatureCategory.Topographical,
            ClassificationType.InsideCorner => FeatureCategory.Topographical,
            ClassificationType.Peninsula => FeatureCategory.Topographical,
            ClassificationType.Island => FeatureCategory.Topographical,
            ClassificationType.PinchPointFunnel => FeatureCategory.Topographical,
            ClassificationType.TravelCorridor => FeatureCategory.Topographical,
            ClassificationType.Spur => FeatureCategory.Topographical,
            ClassificationType.Knob => FeatureCategory.Topographical,

            // Default
            _ => FeatureCategory.Other
        };
    }
}