using System;
using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260818120000_AddCompanyHiringPlans")]
public partial class AddCompanyHiringPlans : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CompanyHiringPlans",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyOwnerUserId = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                JobFamilyId = table.Column<int>(type: "int", nullable: false),
                PositionId = table.Column<int>(type: "int", nullable: false),
                SeniorityId = table.Column<int>(type: "int", nullable: false),
                Headcount = table.Column<int>(type: "int", nullable: false),
                Priority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                TargetStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                EmploymentType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyHiringPlans", x => x.Id);
                table.ForeignKey(
                    name: "FK_CompanyHiringPlans_Users_CompanyOwnerUserId",
                    column: x => x.CompanyOwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyHiringPlans_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyHiringPlans_JobFamilies_JobFamilyId",
                    column: x => x.JobFamilyId,
                    principalTable: "JobFamilies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyHiringPlans_Positions_PositionId",
                    column: x => x.PositionId,
                    principalTable: "Positions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyHiringPlans_Seniorities_SeniorityId",
                    column: x => x.SeniorityId,
                    principalTable: "Seniorities",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.AddColumn<int>(
            name: "HiringPlanId",
            table: "Vacancies",
            type: "int",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_CompanyHiringPlans_CompanyOwnerUserId",
            table: "CompanyHiringPlans",
            column: "CompanyOwnerUserId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyHiringPlans_CreatedByUserId",
            table: "CompanyHiringPlans",
            column: "CreatedByUserId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyHiringPlans_JobFamilyId",
            table: "CompanyHiringPlans",
            column: "JobFamilyId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyHiringPlans_PositionId",
            table: "CompanyHiringPlans",
            column: "PositionId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyHiringPlans_SeniorityId",
            table: "CompanyHiringPlans",
            column: "SeniorityId");
        migrationBuilder.CreateIndex(
            name: "IX_Vacancies_HiringPlanId",
            table: "Vacancies",
            column: "HiringPlanId");

        migrationBuilder.AddForeignKey(
            name: "FK_Vacancies_CompanyHiringPlans_HiringPlanId",
            table: "Vacancies",
            column: "HiringPlanId",
            principalTable: "CompanyHiringPlans",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Vacancies_CompanyHiringPlans_HiringPlanId",
            table: "Vacancies");
        migrationBuilder.DropIndex(
            name: "IX_Vacancies_HiringPlanId",
            table: "Vacancies");
        migrationBuilder.DropColumn(
            name: "HiringPlanId",
            table: "Vacancies");
        migrationBuilder.DropTable(name: "CompanyHiringPlans");
    }
}
