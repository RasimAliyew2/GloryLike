using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260823110000_DecoupleHiringPlanFromTaxonomy")]
public partial class DecoupleHiringPlanFromTaxonomy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "PositionName",
            table: "CompanyHiringPlans",
            type: "nvarchar(160)",
            maxLength: 160,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE plans
            SET plans.PositionName = positions.Name
            FROM dbo.CompanyHiringPlans AS plans
            INNER JOIN dbo.Positions AS positions
                ON positions.Id = plans.PositionId
            WHERE plans.PositionName IS NULL OR plans.PositionName = N'';
            """);

        migrationBuilder.AlterColumn<string>(
            name: "PositionName",
            table: "CompanyHiringPlans",
            type: "nvarchar(160)",
            maxLength: 160,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(160)",
            oldMaxLength: 160,
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "JobFamilyId",
            table: "CompanyHiringPlans",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");

        migrationBuilder.AlterColumn<int>(
            name: "PositionId",
            table: "CompanyHiringPlans",
            type: "int",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "int");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS
            (
                SELECT 1
                FROM dbo.CompanyHiringPlans
                WHERE JobFamilyId IS NULL OR PositionId IS NULL
            )
                THROW 51020, 'Cannot roll back while custom Structure positions are used by Hiring Plan.', 1;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "JobFamilyId",
            table: "CompanyHiringPlans",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.AlterColumn<int>(
            name: "PositionId",
            table: "CompanyHiringPlans",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "PositionName",
            table: "CompanyHiringPlans");
    }
}
