using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

public partial class AddVacancyApplications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "VacancyApplications",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation(
                        "SqlServer:Identity",
                        "1, 1"),
                VacancyId = table.Column<int>(
                    type: "int",
                    nullable: false),
                CandidateUserId = table.Column<int>(
                    type: "int",
                    nullable: false),
                Status = table.Column<string>(
                    type: "nvarchar(30)",
                    maxLength: 30,
                    nullable: false),
                AppliedAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_VacancyApplications",
                    item => item.Id);
                table.ForeignKey(
                    name: "FK_VacancyApplications_Users_CandidateUserId",
                    column: item => item.CandidateUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_VacancyApplications_Vacancies_VacancyId",
                    column: item => item.VacancyId,
                    principalTable: "Vacancies",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_VacancyApplications_CandidateUserId",
            table: "VacancyApplications",
            column: "CandidateUserId");

        migrationBuilder.CreateIndex(
            name: "UX_VacancyApplications_VacancyId_CandidateUserId",
            table: "VacancyApplications",
            columns: new[] { "VacancyId", "CandidateUserId" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "VacancyApplications");
    }
}
