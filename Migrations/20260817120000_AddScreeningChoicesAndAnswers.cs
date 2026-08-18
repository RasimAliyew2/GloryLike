using System;
using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260817120000_AddScreeningChoicesAndAnswers")]
public partial class AddScreeningChoicesAndAnswers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "VacancyScreeningAnswers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                VacancyApplicationId = table.Column<int>(type: "int", nullable: false),
                ScreeningQuestionId = table.Column<int>(type: "int", nullable: false),
                QuestionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                AnswerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                RequirementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                AnswerValueJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                AnswerDisplayText = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                IsCorrect = table.Column<bool>(type: "bit", nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VacancyScreeningAnswers", x => x.Id);
                table.ForeignKey(
                    name: "FK_VacancyScreeningAnswers_VacancyApplications_VacancyApplicationId",
                    column: x => x.VacancyApplicationId,
                    principalTable: "VacancyApplications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "VacancyScreeningChoices",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                ScreeningQuestionId = table.Column<int>(type: "int", nullable: false),
                ChoiceText = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VacancyScreeningChoices", x => x.Id);
                table.ForeignKey(
                    name: "FK_VacancyScreeningChoices_VacancyScreeningQuestions_ScreeningQuestionId",
                    column: x => x.ScreeningQuestionId,
                    principalTable: "VacancyScreeningQuestions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "UX_VacancyScreeningAnswers_ApplicationId_QuestionId",
            table: "VacancyScreeningAnswers",
            columns: new[] { "VacancyApplicationId", "ScreeningQuestionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_VacancyScreeningChoices_QuestionId",
            table: "VacancyScreeningChoices",
            columns: new[] { "ScreeningQuestionId", "SortOrder" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "VacancyScreeningAnswers");
        migrationBuilder.DropTable(name: "VacancyScreeningChoices");
    }
}
