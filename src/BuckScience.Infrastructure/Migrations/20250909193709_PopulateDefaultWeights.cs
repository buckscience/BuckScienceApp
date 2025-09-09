using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PopulateDefaultWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update PropertyFeatures that have NULL Weight with default values based on ClassificationType
            
            // Topographical features - moderate to high importance for movement
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 1"); // Ridge
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 2"); // RidgePoint
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 3"); // RidgeSpur
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.8 WHERE Weight IS NULL AND ClassificationType = 4"); // Saddle
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 5"); // Bench
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 6"); // Draw
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.8 WHERE Weight IS NULL AND ClassificationType = 7"); // CreekCrossing
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.4 WHERE Weight IS NULL AND ClassificationType = 8"); // Ditch
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL AND ClassificationType = 9"); // Valley
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL AND ClassificationType = 10"); // Bluff
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 11"); // FieldEdge
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.8 WHERE Weight IS NULL AND ClassificationType = 12"); // InsideCorner
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 13"); // Peninsula
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL AND ClassificationType = 14"); // Island
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.9 WHERE Weight IS NULL AND ClassificationType = 15"); // PinchPointFunnel
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.8 WHERE Weight IS NULL AND ClassificationType = 16"); // TravelCorridor
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 17"); // Spur
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL AND ClassificationType = 18"); // Knob

            // Food sources - high importance
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.8 WHERE Weight IS NULL AND ClassificationType = 31"); // AgCropField
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.8 WHERE Weight IS NULL AND ClassificationType = 32"); // FoodPlot
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 33"); // MastTreePatch
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 34"); // BrowsePatch
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL AND ClassificationType = 35"); // PrairieForbPatch

            // Water sources - important
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 51"); // Creek
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 52"); // Pond
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 53"); // Lake
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 54"); // Spring
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.8 WHERE Weight IS NULL AND ClassificationType = 55"); // Waterhole
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 56"); // Trough

            // Bedding and cover - critical
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.9 WHERE Weight IS NULL AND ClassificationType = 70"); // BeddingArea
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 71"); // ThickBrush
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL AND ClassificationType = 72"); // Clearcut
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 73"); // CRP
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 74"); // Swamp
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 75"); // CedarThicket
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 76"); // LeewardSlope
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.7 WHERE Weight IS NULL AND ClassificationType = 77"); // EdgeCover
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.6 WHERE Weight IS NULL AND ClassificationType = 78"); // IsolatedCover
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.4 WHERE Weight IS NULL AND ClassificationType = 79"); // ManMadeCover

            // Other
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL AND ClassificationType = 99"); // Other

            // Default weight for any other classification types not covered above
            migrationBuilder.Sql("UPDATE PropertyFeatures SET Weight = 0.5 WHERE Weight IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
