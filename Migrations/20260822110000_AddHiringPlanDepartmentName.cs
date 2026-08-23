using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260822110000_AddHiringPlanDepartmentName")]
public partial class AddHiringPlanDepartmentName : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "DepartmentName",
            table: "CompanyHiringPlans",
            type: "nvarchar(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE plans
            SET plans.DepartmentName = matched.DepartmentName
            FROM dbo.CompanyHiringPlans AS plans
            INNER JOIN dbo.Positions AS taxonomyPosition
                ON taxonomyPosition.Id = plans.PositionId
            CROSS APPLY
            (
                SELECT TOP (1) department.Name AS DepartmentName
                FROM dbo.CompanyStructurePositions AS structurePosition
                INNER JOIN dbo.CompanyStructureDivisions AS division
                    ON division.Id = structurePosition.DivisionId
                INNER JOIN dbo.CompanyStructureDepartments AS department
                    ON department.Id = division.DepartmentId
                WHERE department.CompanyOwnerUserId = plans.CompanyOwnerUserId
                  AND LTRIM(RTRIM(structurePosition.Name)) = LTRIM(RTRIM(taxonomyPosition.Name))
                ORDER BY department.SortOrder, division.SortOrder, structurePosition.SortOrder
            ) AS matched
            WHERE plans.DepartmentName IS NULL OR plans.DepartmentName = N'';

            UPDATE plans
            SET plans.DepartmentName = jobs.JobName
            FROM dbo.CompanyHiringPlans AS plans
            INNER JOIN dbo.JobFamilies AS jobs
                ON jobs.Id = plans.JobFamilyId
            WHERE plans.DepartmentName IS NULL OR plans.DepartmentName = N'';
            """);

        migrationBuilder.AlterColumn<string>(
            name: "DepartmentName",
            table: "CompanyHiringPlans",
            type: "nvarchar(120)",
            maxLength: 120,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(120)",
            oldMaxLength: 120,
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "DepartmentName",
            table: "CompanyHiringPlans");
    }
}
