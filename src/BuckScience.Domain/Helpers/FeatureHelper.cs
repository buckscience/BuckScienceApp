using BuckScience.Domain.Enums;

namespace BuckScience.Domain.Helpers
{
    /// <summary>
    /// Provides metadata and categorization for property features
    /// </summary>
    public static class FeatureHelper
    {
        /// <summary>
        /// Maps ClassificationType values to their corresponding FeatureCategory
        /// </summary>
        public static readonly Dictionary<ClassificationType, FeatureCategory> CategoryMapping = new()
        {
            // Topographical Features
            [ClassificationType.Ridge] = FeatureCategory.Topographical,
            [ClassificationType.RidgePoint] = FeatureCategory.Topographical,
            [ClassificationType.RidgeSpur] = FeatureCategory.Topographical,
            [ClassificationType.Saddle] = FeatureCategory.Topographical,
            [ClassificationType.Bench] = FeatureCategory.Topographical,
            [ClassificationType.Draw] = FeatureCategory.Topographical,
            [ClassificationType.CreekCrossing] = FeatureCategory.Topographical,
            [ClassificationType.Ditch] = FeatureCategory.Topographical,
            [ClassificationType.Valley] = FeatureCategory.Topographical,
            [ClassificationType.Bluff] = FeatureCategory.Topographical,
            [ClassificationType.FieldEdge] = FeatureCategory.Topographical,
            [ClassificationType.InsideCorner] = FeatureCategory.Topographical,
            [ClassificationType.Peninsula] = FeatureCategory.Topographical,
            [ClassificationType.Island] = FeatureCategory.Topographical,
            [ClassificationType.PinchPointFunnel] = FeatureCategory.Topographical,
            [ClassificationType.TravelCorridor] = FeatureCategory.Topographical,
            [ClassificationType.Spur] = FeatureCategory.Topographical,
            [ClassificationType.Knob] = FeatureCategory.Topographical,

            // Food Resources
            [ClassificationType.AgCropField] = FeatureCategory.ResourceFood,
            [ClassificationType.FoodPlot] = FeatureCategory.ResourceFood,
            [ClassificationType.MastTreePatch] = FeatureCategory.ResourceFood,
            [ClassificationType.BrowsePatch] = FeatureCategory.ResourceFood,
            [ClassificationType.PrairieForbPatch] = FeatureCategory.ResourceFood,

            // Water Resources
            [ClassificationType.Creek] = FeatureCategory.ResourceWater,
            [ClassificationType.Pond] = FeatureCategory.ResourceWater,
            [ClassificationType.Lake] = FeatureCategory.ResourceWater,
            [ClassificationType.Spring] = FeatureCategory.ResourceWater,
            [ClassificationType.Waterhole] = FeatureCategory.ResourceWater,
            [ClassificationType.Trough] = FeatureCategory.ResourceWater,

            // Bedding & Cover Resources
            [ClassificationType.BeddingArea] = FeatureCategory.ResourceBedding,
            [ClassificationType.ThickBrush] = FeatureCategory.ResourceBedding,
            [ClassificationType.Clearcut] = FeatureCategory.ResourceBedding,
            [ClassificationType.CRP] = FeatureCategory.ResourceBedding,
            [ClassificationType.Swamp] = FeatureCategory.ResourceBedding,
            [ClassificationType.CedarThicket] = FeatureCategory.ResourceBedding,
            [ClassificationType.LeewardSlope] = FeatureCategory.ResourceBedding,
            [ClassificationType.EdgeCover] = FeatureCategory.ResourceBedding,
            [ClassificationType.IsolatedCover] = FeatureCategory.ResourceBedding,
            [ClassificationType.ManMadeCover] = FeatureCategory.ResourceBedding,

            // Other
            [ClassificationType.Other] = FeatureCategory.Other
        };

        /// <summary>
        /// Feature names for display
        /// </summary>
        public static readonly Dictionary<ClassificationType, string> FeatureNames = new()
        {
            // Topographical Features
            [ClassificationType.Ridge] = "Ridge",
            [ClassificationType.RidgePoint] = "Ridge Point",
            [ClassificationType.RidgeSpur] = "Ridge Spur",
            [ClassificationType.Saddle] = "Saddle",
            [ClassificationType.Bench] = "Bench",
            [ClassificationType.Draw] = "Draw",
            [ClassificationType.CreekCrossing] = "Creek Crossing",
            [ClassificationType.Ditch] = "Ditch",
            [ClassificationType.Valley] = "Valley",
            [ClassificationType.Bluff] = "Bluff",
            [ClassificationType.FieldEdge] = "Field Edge",
            [ClassificationType.InsideCorner] = "Inside Corner",
            [ClassificationType.Peninsula] = "Peninsula",
            [ClassificationType.Island] = "Island",
            [ClassificationType.PinchPointFunnel] = "Pinch Point/Funnel",
            [ClassificationType.TravelCorridor] = "Travel Corridor",
            [ClassificationType.Spur] = "Spur",
            [ClassificationType.Knob] = "Knob",

            // Food Resources
            [ClassificationType.AgCropField] = "Agricultural Crop Field",
            [ClassificationType.FoodPlot] = "Food Plot",
            [ClassificationType.MastTreePatch] = "Mast Tree Patch",
            [ClassificationType.BrowsePatch] = "Browse Patch",
            [ClassificationType.PrairieForbPatch] = "Prairie Forb Patch",

            // Water Resources
            [ClassificationType.Creek] = "Creek",
            [ClassificationType.Pond] = "Pond",
            [ClassificationType.Lake] = "Lake",
            [ClassificationType.Spring] = "Spring",
            [ClassificationType.Waterhole] = "Waterhole",
            [ClassificationType.Trough] = "Trough",

            // Bedding & Cover Resources
            [ClassificationType.BeddingArea] = "Bedding Area",
            [ClassificationType.ThickBrush] = "Thick Brush",
            [ClassificationType.Clearcut] = "Clearcut",
            [ClassificationType.CRP] = "CRP/Conservation Reserve",
            [ClassificationType.Swamp] = "Swamp",
            [ClassificationType.CedarThicket] = "Cedar Thicket",
            [ClassificationType.LeewardSlope] = "Leeward Slope",
            [ClassificationType.EdgeCover] = "Edge Cover",
            [ClassificationType.IsolatedCover] = "Isolated Cover",
            [ClassificationType.ManMadeCover] = "Man-made Cover",

            // Other
            [ClassificationType.Other] = "Other"
        };

        /// <summary>
        /// Feature descriptions for display
        /// </summary>
        public static readonly Dictionary<ClassificationType, string> FeatureDescriptions = new()
        {
            // Topographical Features
            [ClassificationType.Ridge] = "High ground that directs deer movement",
            [ClassificationType.RidgePoint] = "Point extending from a main ridge",
            [ClassificationType.RidgeSpur] = "Secondary ridge extending from main ridge",
            [ClassificationType.Saddle] = "Low point between two ridges or hills",
            [ClassificationType.Bench] = "Flat area on a hillside",
            [ClassificationType.Draw] = "Small drainage or depression",
            [ClassificationType.CreekCrossing] = "Natural crossing point over water",
            [ClassificationType.Ditch] = "Drainage ditch or channel",
            [ClassificationType.Valley] = "Low area between hills or ridges",
            [ClassificationType.Bluff] = "Steep bank or cliff",
            [ClassificationType.FieldEdge] = "Edge of agricultural field",
            [ClassificationType.InsideCorner] = "Inside corner of field or opening",
            [ClassificationType.Peninsula] = "Land extending into water or opening",
            [ClassificationType.Island] = "Isolated high ground or timber",
            [ClassificationType.PinchPointFunnel] = "Natural funnels that concentrate deer movement",
            [ClassificationType.TravelCorridor] = "Paths deer use to move between areas",
            [ClassificationType.Spur] = "Ridge extending from main ridge",
            [ClassificationType.Knob] = "Small rounded hill or elevation",

            // Food Resources
            [ClassificationType.AgCropField] = "Agricultural crops providing food source",
            [ClassificationType.FoodPlot] = "Planted food plots for wildlife",
            [ClassificationType.MastTreePatch] = "Hard or soft mast producing trees",
            [ClassificationType.BrowsePatch] = "Areas with good browse vegetation",
            [ClassificationType.PrairieForbPatch] = "Prairie forbs and wildflowers",

            // Water Resources
            [ClassificationType.Creek] = "Natural flowing water source",
            [ClassificationType.Pond] = "Small body of standing water",
            [ClassificationType.Lake] = "Larger body of standing water",
            [ClassificationType.Spring] = "Natural water source from ground",
            [ClassificationType.Waterhole] = "Small water collection area",
            [ClassificationType.Trough] = "Artificial water source",

            // Bedding & Cover Resources
            [ClassificationType.BeddingArea] = "Areas where deer rest during the day",
            [ClassificationType.ThickBrush] = "Dense brush providing cover",
            [ClassificationType.Clearcut] = "Recently harvested timber area",
            [ClassificationType.CRP] = "Conservation Reserve Program grassland",
            [ClassificationType.Swamp] = "Wetland area providing cover",
            [ClassificationType.CedarThicket] = "Dense cedar trees providing cover",
            [ClassificationType.LeewardSlope] = "Protected slope providing shelter",
            [ClassificationType.EdgeCover] = "Cover along field or opening edges",
            [ClassificationType.IsolatedCover] = "Small isolated patches of cover",
            [ClassificationType.ManMadeCover] = "Artificial cover structures",

            // Other
            [ClassificationType.Other] = "Other important features on the property"
        };

        /// <summary>
        /// Feature icons for display
        /// </summary>
        public static readonly Dictionary<ClassificationType, string> FeatureIcons = new()
        {
            // Topographical Features
            [ClassificationType.Ridge] = "fas fa-mountain",
            [ClassificationType.RidgePoint] = "fas fa-mountain",
            [ClassificationType.RidgeSpur] = "fas fa-mountain",
            [ClassificationType.Saddle] = "fas fa-minus",
            [ClassificationType.Bench] = "fas fa-minus",
            [ClassificationType.Draw] = "fas fa-route",
            [ClassificationType.CreekCrossing] = "fas fa-water",
            [ClassificationType.Ditch] = "fas fa-minus",
            [ClassificationType.Valley] = "fas fa-minus",
            [ClassificationType.Bluff] = "fas fa-mountain",
            [ClassificationType.FieldEdge] = "fas fa-border-style",
            [ClassificationType.InsideCorner] = "fas fa-square",
            [ClassificationType.Peninsula] = "fas fa-map-pin",
            [ClassificationType.Island] = "fas fa-circle",
            [ClassificationType.PinchPointFunnel] = "fas fa-compress-arrows-alt",
            [ClassificationType.TravelCorridor] = "fas fa-route",
            [ClassificationType.Spur] = "fas fa-mountain",
            [ClassificationType.Knob] = "fas fa-circle",

            // Food Resources
            [ClassificationType.AgCropField] = "fas fa-seedling",
            [ClassificationType.FoodPlot] = "fas fa-seedling",
            [ClassificationType.MastTreePatch] = "fas fa-tree",
            [ClassificationType.BrowsePatch] = "fas fa-leaf",
            [ClassificationType.PrairieForbPatch] = "fas fa-seedling",

            // Water Resources
            [ClassificationType.Creek] = "fas fa-water",
            [ClassificationType.Pond] = "fas fa-tint",
            [ClassificationType.Lake] = "fas fa-tint",
            [ClassificationType.Spring] = "fas fa-tint",
            [ClassificationType.Waterhole] = "fas fa-tint",
            [ClassificationType.Trough] = "fas fa-tint",

            // Bedding & Cover Resources
            [ClassificationType.BeddingArea] = "fas fa-bed",
            [ClassificationType.ThickBrush] = "fas fa-tree",
            [ClassificationType.Clearcut] = "fas fa-cut",
            [ClassificationType.CRP] = "fas fa-seedling",
            [ClassificationType.Swamp] = "fas fa-tree",
            [ClassificationType.CedarThicket] = "fas fa-tree",
            [ClassificationType.LeewardSlope] = "fas fa-shield-alt",
            [ClassificationType.EdgeCover] = "fas fa-tree",
            [ClassificationType.IsolatedCover] = "fas fa-tree",
            [ClassificationType.ManMadeCover] = "fas fa-home",

            // Other
            [ClassificationType.Other] = "fas fa-map-pin"
        };

        /// <summary>
        /// Category names for display
        /// </summary>
        public static readonly Dictionary<FeatureCategory, string> CategoryNames = new()
        {
            [FeatureCategory.Topographical] = "Topographical",
            [FeatureCategory.ResourceFood] = "Food Resources",
            [FeatureCategory.ResourceWater] = "Water Resources",
            [FeatureCategory.ResourceBedding] = "Bedding & Cover",
            [FeatureCategory.Other] = "Other"
        };

        /// <summary>
        /// Category descriptions for display
        /// </summary>
        public static readonly Dictionary<FeatureCategory, string> CategoryDescriptions = new()
        {
            [FeatureCategory.Topographical] = "Terrain features that shape deer movement patterns",
            [FeatureCategory.ResourceFood] = "Food sources and feeding areas",
            [FeatureCategory.ResourceWater] = "Water sources and hydration areas",
            [FeatureCategory.ResourceBedding] = "Bedding areas and security cover",
            [FeatureCategory.Other] = "Other miscellaneous features"
        };

        /// <summary>
        /// Gets the category for a classification type
        /// </summary>
        public static FeatureCategory GetCategory(ClassificationType type)
        {
            return CategoryMapping.TryGetValue(type, out var category) ? category : FeatureCategory.Other;
        }

        /// <summary>
        /// Gets the display name for a classification type
        /// </summary>
        public static string GetFeatureName(ClassificationType type)
        {
            return FeatureNames.TryGetValue(type, out var name) ? name : type.ToString();
        }

        /// <summary>
        /// Gets the description for a classification type
        /// </summary>
        public static string GetFeatureDescription(ClassificationType type)
        {
            return FeatureDescriptions.TryGetValue(type, out var description) ? description : $"Features related to {type}";
        }

        /// <summary>
        /// Gets the icon for a classification type
        /// </summary>
        public static string GetFeatureIcon(ClassificationType type)
        {
            return FeatureIcons.TryGetValue(type, out var icon) ? icon : "fas fa-map-pin";
        }

        /// <summary>
        /// Gets the display name for a feature category
        /// </summary>
        public static string GetCategoryName(FeatureCategory category)
        {
            return CategoryNames.TryGetValue(category, out var name) ? name : category.ToString();
        }

        /// <summary>
        /// Gets the description for a feature category
        /// </summary>
        public static string GetCategoryDescription(FeatureCategory category)
        {
            return CategoryDescriptions.TryGetValue(category, out var description) ? description : $"Features in the {category} category";
        }
    }
}