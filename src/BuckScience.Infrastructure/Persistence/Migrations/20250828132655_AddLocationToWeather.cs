using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationToWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "Date",
                table: "Weathers",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<int>(
                name: "Hour",
                table: "Weathers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Weathers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Weathers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_Weather_Location_Date",
                table: "Weathers",
                columns: new[] { "Latitude", "Longitude", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Weather_Location_Date_Hour",
                table: "Weathers",
                columns: new[] { "Latitude", "Longitude", "Date", "Hour" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Weather_Location_Date",
                table: "Weathers");

            migrationBuilder.DropIndex(
                name: "IX_Weather_Location_Date_Hour",
                table: "Weathers");

            migrationBuilder.DropColumn(
                name: "Date",
                table: "Weathers");

            migrationBuilder.DropColumn(
                name: "Hour",
                table: "Weathers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Weathers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Weathers");
        }
    }
}
