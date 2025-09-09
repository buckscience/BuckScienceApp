using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace BuckScience.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AzureEntraB2CId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrialStartDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Properties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Center = table.Column<Point>(type: "geometry", nullable: false),
                    Boundary = table.Column<MultiPolygon>(type: "geometry", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    TimeZone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DayHour = table.Column<int>(type: "int", nullable: false),
                    NightHour = table.Column<int>(type: "int", nullable: false),
                    ApplicationUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Properties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsDefaultTag = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Weathers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Hour = table.Column<int>(type: "int", nullable: false),
                    DateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateTimeEpoch = table.Column<int>(type: "int", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    WindSpeed = table.Column<double>(type: "float", nullable: false),
                    WindDirection = table.Column<double>(type: "float", nullable: false),
                    WindDirectionText = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Visibility = table.Column<double>(type: "float", nullable: false),
                    Pressure = table.Column<double>(type: "float", nullable: false),
                    PressureTrend = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Humidity = table.Column<double>(type: "float", nullable: false),
                    Conditions = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SunriseEpoch = table.Column<int>(type: "int", nullable: false),
                    SunsetEpoch = table.Column<int>(type: "int", nullable: false),
                    CloudCover = table.Column<double>(type: "float", nullable: false),
                    MoonPhase = table.Column<double>(type: "float", nullable: false),
                    MoonPhaseText = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weathers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cameras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    PropertyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cameras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cameras_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PropertyFeatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    ClassificationType = table.Column<int>(type: "int", nullable: false),
                    Geometry = table.Column<Geometry>(type: "geometry", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyFeatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PropertyFeatures_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProfileStatus = table.Column<int>(type: "int", nullable: false),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    CoverPhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Profiles_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyTags",
                columns: table => new
                {
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    IsFastTag = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyTags", x => new { x.PropertyId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PropertyTags_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PropertyTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CameraPlacementHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CameraId = table.Column<int>(type: "int", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    DirectionDegrees = table.Column<float>(type: "real", nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CameraPlacementHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CameraPlacementHistories_Cameras_CameraId",
                        column: x => x.CameraId,
                        principalTable: "Cameras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DateTaken = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DateUploaded = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    PhotoUrl = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    CameraId = table.Column<int>(type: "int", nullable: false),
                    CameraPlacementHistoryId = table.Column<int>(type: "int", nullable: true),
                    WeatherId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Photos_CameraPlacementHistories_CameraPlacementHistoryId",
                        column: x => x.CameraPlacementHistoryId,
                        principalTable: "CameraPlacementHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Photos_Cameras_CameraId",
                        column: x => x.CameraId,
                        principalTable: "Cameras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Photos_Weathers_WeatherId",
                        column: x => x.WeatherId,
                        principalTable: "Weathers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PhotoTags",
                columns: table => new
                {
                    PhotoId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhotoTags", x => new { x.PhotoId, x.TagId });
                    table.ForeignKey(
                        name: "FK_PhotoTags_Photos_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "Photos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PhotoTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ApplicationUsers",
                columns: new[] { "Id", "AzureEntraB2CId", "CreatedDate", "DisplayName", "Email", "FirstName", "LastName", "TrialStartDate" },
                values: new object[] { 1, "b300176c-0f43-4a4d-afd3-d128f8e635a1", new DateTime(2025, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Darrin B", "darrin@buckscience.com", "Darrin", "Brandon", null });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUsers_AzureEntraB2CId",
                table: "ApplicationUsers",
                column: "AzureEntraB2CId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CameraPlacementHistories_CameraId",
                table: "CameraPlacementHistories",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_CameraPlacementHistory_CameraId_EndDateTime",
                table: "CameraPlacementHistories",
                columns: new[] { "CameraId", "EndDateTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Cameras_PropertyId",
                table: "Cameras",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CameraId",
                table: "Photos",
                column: "CameraId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CameraPlacementHistoryId",
                table: "Photos",
                column: "CameraPlacementHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_DateTaken",
                table: "Photos",
                column: "DateTaken");

            migrationBuilder.CreateIndex(
                name: "IX_Photos_WeatherId",
                table: "Photos",
                column: "WeatherId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_PhotoId",
                table: "PhotoTags",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_PhotoTags_TagId",
                table: "PhotoTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_PropertyId",
                table: "Profiles",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_PropertyId_TagId",
                table: "Profiles",
                columns: new[] { "PropertyId", "TagId" });

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_TagId",
                table: "Profiles",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ApplicationUserId",
                table: "Properties",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_ApplicationUserId_Name",
                table: "Properties",
                columns: new[] { "ApplicationUserId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyFeatures_ClassificationType",
                table: "PropertyFeatures",
                column: "ClassificationType");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyFeatures_CreatedBy",
                table: "PropertyFeatures",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyFeatures_PropertyId",
                table: "PropertyFeatures",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTags_PropertyId",
                table: "PropertyTags",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyTags_TagId",
                table: "PropertyTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagName",
                table: "Tags",
                column: "TagName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Weather_Location_Date",
                table: "Weathers",
                columns: new[] { "Latitude", "Longitude", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_Weather_Location_Date_Hour",
                table: "Weathers",
                columns: new[] { "Latitude", "Longitude", "Date", "Hour" });

            migrationBuilder.CreateIndex(
                name: "IX_Weathers_DateTime",
                table: "Weathers",
                column: "DateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Weathers_DateTimeEpoch",
                table: "Weathers",
                column: "DateTimeEpoch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationUsers");

            migrationBuilder.DropTable(
                name: "PhotoTags");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "PropertyFeatures");

            migrationBuilder.DropTable(
                name: "PropertyTags");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "CameraPlacementHistories");

            migrationBuilder.DropTable(
                name: "Weathers");

            migrationBuilder.DropTable(
                name: "Cameras");

            migrationBuilder.DropTable(
                name: "Properties");
        }
    }
}
