using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Photos",
                type: "int",
                nullable: false,
                defaultValue: 1); // Temporary default, will be updated below

            // Update existing photos with correct UserId from Property
            migrationBuilder.Sql(@"
                UPDATE Photos 
                SET UserId = Properties.ApplicationUserId
                FROM Photos 
                INNER JOIN Cameras ON Photos.CameraId = Cameras.Id
                INNER JOIN Properties ON Cameras.PropertyId = Properties.Id
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_UserId",
                table: "Photos",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_UserId",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Photos");
        }
    }
}
