using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureWeights : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<int>(type: "int", nullable: false),
                    ClassificationType = table.Column<int>(type: "int", nullable: false),
                    DefaultWeight = table.Column<float>(type: "real", nullable: false),
                    UserWeight = table.Column<float>(type: "real", nullable: true),
                    SeasonalWeightsJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureWeights_ApplicationUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "ApplicationUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWeights_ApplicationUserId",
                table: "FeatureWeights",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWeights_ClassificationType",
                table: "FeatureWeights",
                column: "ClassificationType");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWeights_User_Classification",
                table: "FeatureWeights",
                columns: new[] { "ApplicationUserId", "ClassificationType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureWeights");
        }
    }
}
