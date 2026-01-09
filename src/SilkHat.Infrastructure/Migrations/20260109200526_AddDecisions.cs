using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilkHat.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    SolutionId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DecisionType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    SubjectKey = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    DiscoveredUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_RepositoryConfigId_SolutionId_DecisionType_Subjec~",
                table: "Decisions",
                columns: new[] { "RepositoryConfigId", "SolutionId", "DecisionType", "SubjectKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Decisions");
        }
    }
}
