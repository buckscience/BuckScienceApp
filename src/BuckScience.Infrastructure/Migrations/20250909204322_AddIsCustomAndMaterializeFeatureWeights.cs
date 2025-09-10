using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsCustomAndMaterializeFeatureWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCustom",
                table: "FeatureWeights",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Set IsCustom = true for existing FeatureWeight records that have customizations
            // (those with UserWeight or SeasonalWeightsJson set)
            migrationBuilder.Sql(@"
                UPDATE FeatureWeights 
                SET IsCustom = 1 
                WHERE UserWeight IS NOT NULL OR SeasonalWeightsJson IS NOT NULL
            ");

            // Materialize feature weights for all existing properties
            // This ensures every property has a row for every feature type
            MaterializeFeatureWeightsForAllProperties(migrationBuilder);
        }

        private void MaterializeFeatureWeightsForAllProperties(MigrationBuilder migrationBuilder)
        {
            // All classification types except 'Other' (99)
            var classificationTypes = new[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, // Topographical
                31, 32, 33, 34, 35, // Food
                51, 52, 53, 54, 55, 56, // Water  
                70, 71, 72, 73, 74, 75, 76, 77, 78, 79 // Bedding & Cover
            };

            // Default weights mapping based on FeatureWeightHelper.GetDefaultWeight()
            var defaultWeights = new Dictionary<int, float>
            {
                // Food sources - high importance
                { 31, 0.8f }, // AgCropField
                { 32, 0.8f }, // FoodPlot
                { 33, 0.7f }, // MastTreePatch
                { 34, 0.6f }, // BrowsePatch
                { 35, 0.5f }, // PrairieForbPatch

                // Water sources - important
                { 51, 0.6f }, // Creek
                { 52, 0.7f }, // Pond
                { 53, 0.6f }, // Lake
                { 54, 0.7f }, // Spring
                { 55, 0.8f }, // Waterhole
                { 56, 0.6f }, // Trough

                // Bedding and cover - critical
                { 70, 0.9f }, // BeddingArea
                { 71, 0.7f }, // ThickBrush
                { 72, 0.5f }, // Clearcut
                { 73, 0.6f }, // CRP
                { 74, 0.6f }, // Swamp
                { 75, 0.7f }, // CedarThicket
                { 76, 0.6f }, // LeewardSlope
                { 77, 0.7f }, // EdgeCover
                { 78, 0.6f }, // IsolatedCover
                { 79, 0.4f }, // ManMadeCover

                // Topographical features - moderate to high importance
                { 1, 0.6f },  // Ridge
                { 2, 0.7f },  // RidgePoint
                { 3, 0.6f },  // RidgeSpur
                { 4, 0.8f },  // Saddle
                { 5, 0.6f },  // Bench
                { 6, 0.7f },  // Draw
                { 7, 0.8f },  // CreekCrossing
                { 8, 0.4f },  // Ditch
                { 9, 0.5f },  // Valley
                { 10, 0.5f }, // Bluff
                { 11, 0.7f }, // FieldEdge
                { 12, 0.8f }, // InsideCorner
                { 13, 0.6f }, // Peninsula
                { 14, 0.5f }, // Island
                { 15, 0.9f }, // PinchPointFunnel
                { 16, 0.8f }, // TravelCorridor
                { 17, 0.6f }, // Spur
                { 18, 0.5f }  // Knob
            };

            // For each property, insert missing FeatureWeight records
            foreach (var classificationType in classificationTypes)
            {
                var defaultWeight = defaultWeights[classificationType];

                migrationBuilder.Sql($@"
                    INSERT INTO FeatureWeights (PropertyId, ClassificationType, DefaultWeight, UserWeight, SeasonalWeightsJson, IsCustom, UpdatedAt)
                    SELECT p.Id, {classificationType}, {defaultWeight:F1}, NULL, NULL, 0, GETUTCDATE()
                    FROM Properties p
                    WHERE NOT EXISTS (
                        SELECT 1 FROM FeatureWeights fw 
                        WHERE fw.PropertyId = p.Id AND fw.ClassificationType = {classificationType}
                    )
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCustom",
                table: "FeatureWeights");
        }
    }
}
