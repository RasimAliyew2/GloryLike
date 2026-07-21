using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations
{
    /// <inheritdoc />
    public partial class Vacancies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vacancies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployerUserId = table.Column<int>(type: "int", nullable: false),
                    PlatformVacancyId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    JobFamilyId = table.Column<int>(type: "int", nullable: false),
                    SeniorityId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    JobFamilyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SeniorityName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ClientRequisitionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmploymentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExperienceRequired = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EducationRequirement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EducationLevel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MinSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentTerms = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    HideSalary = table.Column<bool>(type: "bit", nullable: false),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    MinimumVerificationLevel = table.Column<int>(type: "int", nullable: false),
                    MinimumMatchScore = table.Column<int>(type: "int", nullable: false),
                    MinimumTrustScore = table.Column<int>(type: "int", nullable: false),
                    AutoRejectBelowScore = table.Column<bool>(type: "bit", nullable: false),
                    RequireVerifiedCoreSkills = table.Column<bool>(type: "bit", nullable: false),
                    ScreeningNotes = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    StageApplied = table.Column<bool>(type: "bit", nullable: false),
                    StageScreening = table.Column<bool>(type: "bit", nullable: false),
                    StageInterview = table.Column<bool>(type: "bit", nullable: false),
                    StageOffer = table.Column<bool>(type: "bit", nullable: false),
                    InterviewRounds = table.Column<int>(type: "int", nullable: false),
                    ScreeningSlaDays = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PublishDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApplicationDeadline = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AllowInternalCandidates = table.Column<bool>(type: "bit", nullable: false),
                    NotifyMatchingCandidates = table.Column<bool>(type: "bit", nullable: false),
                    PublicationPriority = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SourcePayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacancies_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalTable: "JobFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Seniorities_SeniorityId",
                        column: x => x.SeniorityId,
                        principalTable: "Seniorities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vacancies_Users_EmployerUserId",
                        column: x => x.EmployerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VacancyApplicationRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacancyId = table.Column<int>(type: "int", nullable: false),
                    FieldKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequirementMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsCustom = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyApplicationRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyApplicationRequirements_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacancyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyBenefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyBenefits_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyFunnelStages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacancyId = table.Column<int>(type: "int", nullable: false),
                    StageName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Hours = table.Column<int>(type: "int", nullable: false),
                    IsStandard = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyFunnelStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyFunnelStages_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyPublicationChannels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacancyId = table.Column<int>(type: "int", nullable: false),
                    ChannelType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ChannelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyPublicationChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyPublicationChannels_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancyScreeningQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacancyId = table.Column<int>(type: "int", nullable: false),
                    QuestionText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AnswerType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequirementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancyScreeningQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancyScreeningQuestions_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacancySkillRequirements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VacancyId = table.Column<int>(type: "int", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinimumVerificationLevel = table.Column<int>(type: "int", nullable: false),
                    RequirementType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacancySkillRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacancySkillRequirements_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VacancySkillRequirements_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_CreatedAtUtc",
                table: "Vacancies",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_EmployerUserId",
                table: "Vacancies",
                column: "EmployerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_JobFamilyId",
                table: "Vacancies",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_PositionId",
                table: "Vacancies",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_SeniorityId",
                table: "Vacancies",
                column: "SeniorityId");

            migrationBuilder.CreateIndex(
                name: "UX_Vacancies_PlatformVacancyId",
                table: "Vacancies",
                column: "PlatformVacancyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_VacancyApplicationRequirements_VacancyId_FieldKey",
                table: "VacancyApplicationRequirements",
                columns: new[] { "VacancyId", "FieldKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_VacancyBenefits_VacancyId_Name",
                table: "VacancyBenefits",
                columns: new[] { "VacancyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacancyFunnelStages_VacancyId",
                table: "VacancyFunnelStages",
                columns: new[] { "VacancyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "UX_VacancyPublicationChannels_VacancyId_ChannelName",
                table: "VacancyPublicationChannels",
                columns: new[] { "VacancyId", "ChannelName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VacancyScreeningQuestions_VacancyId",
                table: "VacancyScreeningQuestions",
                columns: new[] { "VacancyId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_VacancySkillRequirements_SkillId",
                table: "VacancySkillRequirements",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "UX_VacancySkillRequirements_VacancyId_SkillId",
                table: "VacancySkillRequirements",
                columns: new[] { "VacancyId", "SkillId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VacancyApplicationRequirements");

            migrationBuilder.DropTable(
                name: "VacancyBenefits");

            migrationBuilder.DropTable(
                name: "VacancyFunnelStages");

            migrationBuilder.DropTable(
                name: "VacancyPublicationChannels");

            migrationBuilder.DropTable(
                name: "VacancyScreeningQuestions");

            migrationBuilder.DropTable(
                name: "VacancySkillRequirements");

            migrationBuilder.DropTable(
                name: "Vacancies");
        }
    }
}
