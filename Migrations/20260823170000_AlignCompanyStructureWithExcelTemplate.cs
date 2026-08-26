using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260823170000_AlignCompanyStructureWithExcelTemplate")]
public partial class AlignCompanyStructureWithExcelTemplate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Headcount",
            table: "CompanyStructurePositions",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<string>(
            name: "ReportsTo",
            table: "CompanyStructurePositions",
            type: "nvarchar(160)",
            maxLength: 160,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "Seniority",
            table: "CompanyStructurePositions",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Not specified");

        migrationBuilder.AddCheckConstraint(
            name: "CK_CompanyStructurePositions_Headcount",
            table: "CompanyStructurePositions",
            sql: "[Headcount] >= 1 AND [Headcount] <= 10000");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_CompanyStructurePositions_Headcount",
            table: "CompanyStructurePositions");

        migrationBuilder.DropColumn(
            name: "Headcount",
            table: "CompanyStructurePositions");

        migrationBuilder.DropColumn(
            name: "ReportsTo",
            table: "CompanyStructurePositions");

        migrationBuilder.DropColumn(
            name: "Seniority",
            table: "CompanyStructurePositions");
    }
}
