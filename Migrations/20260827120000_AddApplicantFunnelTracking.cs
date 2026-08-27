using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260827120000_AddApplicantFunnelTracking")]
public partial class AddApplicantFunnelTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FunnelStageName",
            table: "VacancyApplications",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<DateTime>(
            name: "FunnelStageUpdatedAtUtc",
            table: "VacancyApplications",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "HiredAtUtc",
            table: "VacancyApplications",
            type: "datetime2",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE application
            SET
                FunnelStageName = COALESCE(initialStage.StageName, N'Applied'),
                FunnelStageUpdatedAtUtc = COALESCE(
                    application.UpdatedAtUtc,
                    application.AppliedAtUtc)
            FROM VacancyApplications AS application
            OUTER APPLY
            (
                SELECT TOP (1) stage.StageName
                FROM VacancyFunnelStages AS stage
                WHERE stage.VacancyId = application.VacancyId
                ORDER BY stage.SortOrder, stage.Id
            ) AS initialStage
            WHERE application.FunnelStageName = N'';
            """);

        migrationBuilder.CreateIndex(
            name: "IX_VacancyApplications_HiredAtUtc",
            table: "VacancyApplications",
            column: "HiredAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_VacancyApplications_VacancyId_FunnelStageName",
            table: "VacancyApplications",
            columns: new[] { "VacancyId", "FunnelStageName" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_VacancyApplications_HiredAtUtc",
            table: "VacancyApplications");

        migrationBuilder.DropIndex(
            name: "IX_VacancyApplications_VacancyId_FunnelStageName",
            table: "VacancyApplications");

        migrationBuilder.DropColumn(
            name: "FunnelStageName",
            table: "VacancyApplications");

        migrationBuilder.DropColumn(
            name: "FunnelStageUpdatedAtUtc",
            table: "VacancyApplications");

        migrationBuilder.DropColumn(
            name: "HiredAtUtc",
            table: "VacancyApplications");
    }
}
