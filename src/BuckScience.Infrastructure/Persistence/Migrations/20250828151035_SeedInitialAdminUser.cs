using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TrialStartDate",
                table: "ApplicationUsers",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.InsertData(
                table: "ApplicationUsers",
                columns: new[] { "Id", "AzureEntraB2CId", "CreatedDate", "DisplayName", "Email", "FirstName", "LastName", "TrialStartDate" },
                values: new object[] { 1, "b300176c-0f43-4a4d-afd3-d128f8e635a1", new DateTime(2025, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Darrin B", "darrin@buckscience.com", "Darrin", "Brandon", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ApplicationUsers",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.AlterColumn<DateTime>(
                name: "TrialStartDate",
                table: "ApplicationUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);
        }
    }
}
