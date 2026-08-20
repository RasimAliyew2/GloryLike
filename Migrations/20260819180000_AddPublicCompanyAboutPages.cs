using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819180000_AddPublicCompanyAboutPages")]
public partial class AddPublicCompanyAboutPages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AboutPageCustomHtml",
            table: "CompanyProfiles",
            type: "nvarchar(max)",
            maxLength: 60000,
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<string>(
            name: "AboutPageLayoutJson",
            table: "CompanyProfiles",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: false,
            defaultValue: "[\"media\",\"about\",\"culture\",\"benefits\",\"locations\",\"vacancies\",\"contact\"]");

        migrationBuilder.AddColumn<string>(
            name: "CoverImageDataUrl",
            table: "CompanyProfiles",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<bool>(
            name: "UseCustomAboutPageHtml",
            table: "CompanyProfiles",
            type: "bit",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "AboutPageCustomHtml",
            table: "CompanyProfiles");

        migrationBuilder.DropColumn(
            name: "AboutPageLayoutJson",
            table: "CompanyProfiles");

        migrationBuilder.DropColumn(
            name: "CoverImageDataUrl",
            table: "CompanyProfiles");

        migrationBuilder.DropColumn(
            name: "UseCustomAboutPageHtml",
            table: "CompanyProfiles");
    }
}
