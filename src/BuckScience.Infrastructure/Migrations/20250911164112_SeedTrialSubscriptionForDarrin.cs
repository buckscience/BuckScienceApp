using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuckScience.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedTrialSubscriptionForDarrin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Subscriptions",
                columns: new[] { "Id", "CanceledAt", "CreatedAt", "CurrentPeriodEnd", "CurrentPeriodStart", "Status", "StripeCustomerId", "StripeSubscriptionId", "Tier", "UserId" },
                values: new object[] { 1, null, new DateTime(2025, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 2, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), "active", null, null, 0, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Subscriptions",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
