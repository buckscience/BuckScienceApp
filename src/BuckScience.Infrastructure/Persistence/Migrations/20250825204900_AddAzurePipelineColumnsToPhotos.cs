using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAzurePipelineColumnsToPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Azure pipeline columns to existing Photos table
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Photos",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "Photos",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbBlobName",
                table: "Photos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayBlobName",
                table: "Photos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TakenAtUtc",
                table: "Photos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "Photos",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "Photos",
                type: "decimal(11,8)",
                precision: 11,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WeatherJson",
                table: "Photos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Photos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Add indexes for Azure pipeline columns
            migrationBuilder.CreateIndex(
                name: "IX_Photos_UserId",
                table: "Photos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_ContentHash",
                table: "Photos",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_Status",
                table: "Photos",
                column: "Status");

            // Add unique constraint to prevent duplicate uploads (when using Azure pipeline)
            migrationBuilder.CreateIndex(
                name: "IX_Photos_UserId_CameraId_ContentHash",
                table: "Photos",
                columns: new[] { "UserId", "CameraId", "ContentHash" },
                unique: true,
                filter: "[UserId] IS NOT NULL AND [ContentHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove indexes
            migrationBuilder.DropIndex(
                name: "IX_Photos_UserId_CameraId_ContentHash",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_Status",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_ContentHash",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_UserId",
                table: "Photos");

            // Remove Azure pipeline columns
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ThumbBlobName",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "DisplayBlobName",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "TakenAtUtc",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "WeatherJson",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Photos");
        }
    }
}