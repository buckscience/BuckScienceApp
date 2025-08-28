using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDefaultTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyTag_Properties_PropertyId",
                table: "PropertyTag");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyTag_Tags_TagId",
                table: "PropertyTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PropertyTag",
                table: "PropertyTag");

            migrationBuilder.RenameTable(
                name: "PropertyTag",
                newName: "PropertyTags");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyTag_TagId",
                table: "PropertyTags",
                newName: "IX_PropertyTags_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyTag_PropertyId",
                table: "PropertyTags",
                newName: "IX_PropertyTags_PropertyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PropertyTags",
                table: "PropertyTags",
                columns: new[] { "PropertyId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyTags_Properties_PropertyId",
                table: "PropertyTags",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyTags_Tags_TagId",
                table: "PropertyTags",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Seed default tags
            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "TagName", "IsDefaultTag" },
                values: new object[,]
                {
                    { "deer", true },
                    { "turkey", true },
                    { "bear", true },
                    { "buck", true },
                    { "doe", true },
                    { "predator", true }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PropertyTags_Properties_PropertyId",
                table: "PropertyTags");

            migrationBuilder.DropForeignKey(
                name: "FK_PropertyTags_Tags_TagId",
                table: "PropertyTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PropertyTags",
                table: "PropertyTags");

            migrationBuilder.RenameTable(
                name: "PropertyTags",
                newName: "PropertyTag");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyTags_TagId",
                table: "PropertyTag",
                newName: "IX_PropertyTag_TagId");

            migrationBuilder.RenameIndex(
                name: "IX_PropertyTags_PropertyId",
                table: "PropertyTag",
                newName: "IX_PropertyTag_PropertyId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PropertyTag",
                table: "PropertyTag",
                columns: new[] { "PropertyId", "TagId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyTag_Properties_PropertyId",
                table: "PropertyTag",
                column: "PropertyId",
                principalTable: "Properties",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PropertyTag_Tags_TagId",
                table: "PropertyTag",
                column: "TagId",
                principalTable: "Tags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            // Remove default tags
            migrationBuilder.DeleteData(
                table: "Tags",
                keyColumn: "TagName",
                keyValues: new object[] { "deer", "turkey", "bear", "buck", "doe", "predator" });
        }
    }
}
