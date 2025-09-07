using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCameraPlacementHistoryToPhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CameraPlacementHistoryId",
                table: "Photos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CameraPlacementHistoryId",
                table: "Photos",
                column: "CameraPlacementHistoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Photos_CameraPlacementHistories_CameraPlacementHistoryId",
                table: "Photos",
                column: "CameraPlacementHistoryId",
                principalTable: "CameraPlacementHistories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Photos_CameraPlacementHistories_CameraPlacementHistoryId",
                table: "Photos");

            migrationBuilder.DropIndex(
                name: "IX_Photos_CameraPlacementHistoryId",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "CameraPlacementHistoryId",
                table: "Photos");
        }
    }
}
