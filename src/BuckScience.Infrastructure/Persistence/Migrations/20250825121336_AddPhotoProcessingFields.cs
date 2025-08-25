using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPhotoProcessingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FileSizeBytes",
                table: "Photos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ThumbnailSizeBytes",
                table: "Photos",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Photos",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileSizeBytes",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ThumbnailSizeBytes",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Photos");
        }
    }
}
