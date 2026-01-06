using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilkHat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositorySolutionIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SolutionId",
                table: "RepositorySolutionConfigs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SolutionId",
                table: "RepositorySolutionConfigs");
        }
    }
}
