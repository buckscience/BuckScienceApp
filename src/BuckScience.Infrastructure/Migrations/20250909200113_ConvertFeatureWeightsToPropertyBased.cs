using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConvertFeatureWeightsToPropertyBased : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Clear existing FeatureWeights data since we're changing the association from user-based to property-based
            // This is a breaking change but aligns with the requirement that weights should be property-specific
            migrationBuilder.Sql("DELETE FROM FeatureWeights");

            // Step 2: Drop the foreign key constraint
            migrationBuilder.DropForeignKey(
                name: "FK_FeatureWeights_ApplicationUsers_ApplicationUserId",
                table: "FeatureWeights");

            // Step 3: Rename the column and indexes
            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "FeatureWeights",
                newName: "PropertyId");

            migrationBuilder.RenameIndex(
                name: "IX_FeatureWeights_User_Classification",
                table: "FeatureWeights",
                newName: "IX_FeatureWeights_Property_Classification");

            migrationBuilder.RenameIndex(
                name: "IX_FeatureWeights_ApplicationUserId",
                table: "FeatureWeights",
                newName: "IX_FeatureWeights_PropertyId");

            // Step 4: Add the new foreign key constraint to Properties table
            migrationBuilder.AddForeignKey(
                name: "FK_FeatureWeights_Properties_PropertyId",
                table: "FeatureWeights",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeatureWeights_Properties_PropertyId",
                table: "FeatureWeights");

            migrationBuilder.RenameColumn(
                name: "PropertyId",
                table: "FeatureWeights",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_FeatureWeights_PropertyId",
                table: "FeatureWeights",
                newName: "IX_FeatureWeights_ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_FeatureWeights_Property_Classification",
                table: "FeatureWeights",
                newName: "IX_FeatureWeights_User_Classification");

            migrationBuilder.AddForeignKey(
                name: "FK_FeatureWeights_ApplicationUsers_ApplicationUserId",
                table: "FeatureWeights",
                column: "ApplicationUserId",
                principalTable: "ApplicationUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
