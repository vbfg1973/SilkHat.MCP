using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilkHat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositorySolutionConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepositorySolutionConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    RepositoryConfigId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepositorySolutionConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RepositorySolutionConfigs_RepositoryConfigs_RepositoryConfigId",
                        column: x => x.RepositoryConfigId,
                        principalTable: "RepositoryConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepositorySolutionConfigs_RepositoryConfigId",
                table: "RepositorySolutionConfigs",
                column: "RepositoryConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepositorySolutionConfigs");
        }
    }
}
