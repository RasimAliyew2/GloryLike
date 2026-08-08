using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260806190000_AddSharedCompanyOrganization")]
public partial class AddSharedCompanyOrganization : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "CompanyOwnerUserId",
            table: "Vacancies",
            type: "int",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE v
            SET CompanyOwnerUserId = COALESCE(
                (
                    SELECT TOP (1) invitation.OwnerUserId
                    FROM CompanyTeamInvitations invitation
                    WHERE invitation.AcceptedUserId = v.EmployerUserId
                    ORDER BY invitation.AcceptedAtUtc DESC
                ),
                v.EmployerUserId)
            FROM Vacancies v;
            """);

        migrationBuilder.AlterColumn<int>(
            name: "CompanyOwnerUserId",
            table: "Vacancies",
            type: "int",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "int",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "CompanyProfiles",
            columns: table => new
            {
                Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                    .Annotation(
                        "SqlServer:Identity",
                        "1, 1"),
                OwnerUserId = table.Column<int>(
                    type: "int",
                    nullable: false),
                CompanyName = table.Column<string>(
                    type: "nvarchar(160)",
                    maxLength: 160,
                    nullable: false),
                CompanyType = table.Column<string>(
                    type: "nvarchar(40)",
                    maxLength: 40,
                    nullable: false),
                ActivityScope = table.Column<string>(
                    type: "nvarchar(120)",
                    maxLength: 120,
                    nullable: false),
                FoundationYear = table.Column<int>(
                    type: "int",
                    nullable: true),
                EmployeeCount = table.Column<string>(
                    type: "nvarchar(30)",
                    maxLength: 30,
                    nullable: false),
                Website = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                PageLanguage = table.Column<string>(
                    type: "nvarchar(40)",
                    maxLength: 40,
                    nullable: false),
                CompanyVideo = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                CompanyDescription = table.Column<string>(
                    type: "nvarchar(2500)",
                    maxLength: 2500,
                    nullable: false),
                CompanyCulture = table.Column<string>(
                    type: "nvarchar(1600)",
                    maxLength: 1600,
                    nullable: false),
                WhyWorkWithUs = table.Column<string>(
                    type: "nvarchar(1600)",
                    maxLength: 1600,
                    nullable: false),
                BenefitsJson = table.Column<string>(
                    type: "nvarchar(max)",
                    nullable: false),
                CompanyAddress = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                CompanyCountry = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false),
                CompanyCity = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false),
                LinkedInUrl = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                InstagramUrl = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                FacebookUrl = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                YoutubeUrl = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                TelegramUrl = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                TiktokUrl = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                UpdatedByUserId = table.Column<int>(
                    type: "int",
                    nullable: false),
                CreatedAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_CompanyProfiles",
                    item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyProfiles_Users_OwnerUserId",
                    column: item => item.OwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyProfiles_Users_UpdatedByUserId",
                    column: item => item.UpdatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Vacancies_CompanyOwnerUserId",
            table: "Vacancies",
            column: "CompanyOwnerUserId");

        migrationBuilder.CreateIndex(
            name: "UX_CompanyProfiles_OwnerUserId",
            table: "CompanyProfiles",
            column: "OwnerUserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CompanyProfiles_UpdatedByUserId",
            table: "CompanyProfiles",
            column: "UpdatedByUserId");

        migrationBuilder.AddForeignKey(
            name: "FK_Vacancies_Users_CompanyOwnerUserId",
            table: "Vacancies",
            column: "CompanyOwnerUserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "CompanyProfiles");

        migrationBuilder.DropForeignKey(
            name: "FK_Vacancies_Users_CompanyOwnerUserId",
            table: "Vacancies");

        migrationBuilder.DropIndex(
            name: "IX_Vacancies_CompanyOwnerUserId",
            table: "Vacancies");

        migrationBuilder.DropColumn(
            name: "CompanyOwnerUserId",
            table: "Vacancies");
    }
}
