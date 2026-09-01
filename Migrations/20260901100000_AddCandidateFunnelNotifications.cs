using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901100000_AddCandidateFunnelNotifications")]
public partial class AddCandidateFunnelNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CandidateNotifications",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CandidateUserId = table.Column<int>(type: "int", nullable: false),
                VacancyId = table.Column<int>(type: "int", nullable: false),
                VacancyApplicationId = table.Column<int>(type: "int", nullable: false),
                Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Title = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Message = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                IsRead = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CandidateNotifications", item => item.Id);
                table.ForeignKey(
                    name: "FK_CandidateNotifications_Users_CandidateUserId",
                    column: item => item.CandidateUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CandidateNotifications_Vacancies_VacancyId",
                    column: item => item.VacancyId,
                    principalTable: "Vacancies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_CandidateNotifications_VacancyApplications_VacancyApplicationId",
                    column: item => item.VacancyApplicationId,
                    principalTable: "VacancyApplications",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_CandidateNotifications_Candidate_Read_CreatedAt",
            table: "CandidateNotifications",
            columns: new[] { "CandidateUserId", "IsRead", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_CandidateNotifications_VacancyApplicationId",
            table: "CandidateNotifications",
            column: "VacancyApplicationId");
        migrationBuilder.CreateIndex(
            name: "IX_CandidateNotifications_VacancyId",
            table: "CandidateNotifications",
            column: "VacancyId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CandidateNotifications");
    }
}
