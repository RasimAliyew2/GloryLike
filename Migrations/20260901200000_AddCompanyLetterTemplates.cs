using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260901200000_AddCompanyLetterTemplates")]
public partial class AddCompanyLetterTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CompanyLetterTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CompanyOwnerUserId = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                DefaultKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Audience = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Category = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Subject = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Body = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyLetterTemplates", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyLetterTemplates_Users_CompanyOwnerUserId",
                    column: item => item.CompanyOwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyLetterTemplates_Users_CreatedByUserId",
                    column: item => item.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CompanyLetterTemplates_CompanyOwnerUserId",
            table: "CompanyLetterTemplates",
            column: "CompanyOwnerUserId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyLetterTemplates_CreatedByUserId",
            table: "CompanyLetterTemplates",
            column: "CreatedByUserId");
        migrationBuilder.CreateIndex(
            name: "UX_CompanyLetterTemplates_CompanyOwner_DefaultKey",
            table: "CompanyLetterTemplates",
            columns: new[] { "CompanyOwnerUserId", "DefaultKey" },
            unique: true,
            filter: "[DefaultKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CompanyLetterTemplates");
    }
}
