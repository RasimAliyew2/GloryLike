using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillIdToSkillQuestionnaires : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillQuestionnaires",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<int>(type: "int", nullable: true),
                    SkillName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Seniority = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SkillComplexity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    QuestionCount = table.Column<int>(type: "int", nullable: false),
                    StructureJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    GeneratedByModel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillQuestionnaires", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillQuestionnaires_CacheLookup",
                table: "SkillQuestionnaires",
                columns: new[] { "SkillName", "Seniority", "SkillComplexity", "Language", "Version", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillQuestionnaires");
        }
    }
}
