using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilkHat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositorySolutionConfigTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedUtc",
                table: "RepositorySolutionConfigs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedUtc",
                table: "RepositorySolutionConfigs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedUtc",
                table: "RepositorySolutionConfigs");

            migrationBuilder.DropColumn(
                name: "UpdatedUtc",
                table: "RepositorySolutionConfigs");
        }
    }
}
