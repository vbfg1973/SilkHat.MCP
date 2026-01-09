using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilkHat.Infrastructure.Migrations;

public partial class AddMethodImplementationDecisions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MethodImplementationDecisions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RepositoryConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                SolutionId = table.Column<string>(type: "text", nullable: false),
                InterfaceTypeName = table.Column<string>(type: "text", nullable: false),
                InterfaceMethodSignature = table.Column<string>(type: "text", nullable: false),
                ImplementationTypeName = table.Column<string>(type: "text", nullable: false),
                CreatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MethodImplementationDecisions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_MethodImplementationDecisions_RepositoryConfigId_SolutionId_InterfaceMethodSignature",
            table: "MethodImplementationDecisions",
            columns: new[] { "RepositoryConfigId", "SolutionId", "InterfaceMethodSignature" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "MethodImplementationDecisions");
    }
}
