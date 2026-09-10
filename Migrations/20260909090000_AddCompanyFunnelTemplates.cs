using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260909090000_AddCompanyFunnelTemplates")]
public partial class AddCompanyFunnelTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "ResponsibleRole", table: "VacancyFunnelStages",
            type: "nvarchar(40)", maxLength: 40, nullable: false, defaultValue: "Recruiter");

        migrationBuilder.CreateTable(
            name: "CompanyFunnelTemplates",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CompanyOwnerUserId = table.Column<int>(type: "int", nullable: false),
                CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                DefaultKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                StagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyFunnelTemplates", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyFunnelTemplates_Users_CompanyOwnerUserId",
                    column: item => item.CompanyOwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyFunnelTemplates_Users_CreatedByUserId",
                    column: item => item.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CompanyFunnelTemplates_CompanyOwnerUserId",
            table: "CompanyFunnelTemplates",
            column: "CompanyOwnerUserId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyFunnelTemplates_CreatedByUserId",
            table: "CompanyFunnelTemplates",
            column: "CreatedByUserId");
        migrationBuilder.CreateIndex(
            name: "UX_CompanyFunnelTemplates_CompanyOwner_DefaultKey",
            table: "CompanyFunnelTemplates",
            columns: new[] { "CompanyOwnerUserId", "DefaultKey" },
            unique: true,
            filter: "[DefaultKey] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CompanyFunnelTemplates");
        migrationBuilder.DropColumn(name: "ResponsibleRole", table: "VacancyFunnelStages");
    }
}
