using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SilkHat.Infrastructure.Migrations;

public partial class AddMethodImplementationDecisionDocIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ImplementationMethodDocumentationId",
            table: "MethodImplementationDecisions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ImplementationTypeDocumentationId",
            table: "MethodImplementationDecisions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InterfaceMethodDocumentationId",
            table: "MethodImplementationDecisions",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "InterfaceTypeDocumentationId",
            table: "MethodImplementationDecisions",
            type: "text",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ImplementationMethodDocumentationId",
            table: "MethodImplementationDecisions");

        migrationBuilder.DropColumn(
            name: "ImplementationTypeDocumentationId",
            table: "MethodImplementationDecisions");

        migrationBuilder.DropColumn(
            name: "InterfaceMethodDocumentationId",
            table: "MethodImplementationDecisions");

        migrationBuilder.DropColumn(
            name: "InterfaceTypeDocumentationId",
            table: "MethodImplementationDecisions");
    }
}
