using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260819120000_AddCompanyLocationsAndLogo")]
public partial class AddCompanyLocationsAndLogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "LogoDataUrl",
            table: "CompanyProfiles",
            type: "nvarchar(max)",
            nullable: false,
            defaultValue: "");

        migrationBuilder.AddColumn<int>(
            name: "CompanyLocationId",
            table: "Vacancies",
            type: "int",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LocationName",
            table: "Vacancies",
            type: "nvarchar(460)",
            maxLength: 460,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "CompanyLocations",
            columns: table => new
            {
                Id = table.Column<int>(
                        type: "int",
                        nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyProfileId = table.Column<int>(
                    type: "int",
                    nullable: false),
                Name = table.Column<string>(
                    type: "nvarchar(120)",
                    maxLength: 120,
                    nullable: false),
                Address = table.Column<string>(
                    type: "nvarchar(240)",
                    maxLength: 240,
                    nullable: false),
                Country = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false),
                City = table.Column<string>(
                    type: "nvarchar(100)",
                    maxLength: 100,
                    nullable: false),
                SortOrder = table.Column<int>(
                    type: "int",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyLocations", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyLocations_CompanyProfiles_CompanyProfileId",
                    column: item => item.CompanyProfileId,
                    principalTable: "CompanyProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.Sql(
            """
            INSERT INTO dbo.CompanyLocations
                (CompanyProfileId, Name, Address, Country, City, SortOrder)
            SELECT
                profile.Id,
                N'',
                profile.CompanyAddress,
                profile.CompanyCountry,
                profile.CompanyCity,
                0
            FROM dbo.CompanyProfiles AS profile
            WHERE NULLIF(LTRIM(RTRIM(profile.CompanyAddress)), N'') IS NOT NULL
               OR NULLIF(LTRIM(RTRIM(profile.CompanyCountry)), N'') IS NOT NULL
               OR NULLIF(LTRIM(RTRIM(profile.CompanyCity)), N'') IS NOT NULL;
            """);

        migrationBuilder.CreateIndex(
            name: "IX_CompanyLocations_CompanyProfileId",
            table: "CompanyLocations",
            column: "CompanyProfileId");

        migrationBuilder.CreateIndex(
            name: "IX_Vacancies_CompanyLocationId",
            table: "Vacancies",
            column: "CompanyLocationId");

        migrationBuilder.AddForeignKey(
            name: "FK_Vacancies_CompanyLocations_CompanyLocationId",
            table: "Vacancies",
            column: "CompanyLocationId",
            principalTable: "CompanyLocations",
            principalColumn: "Id",
            onDelete: ReferentialAction.SetNull);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_Vacancies_CompanyLocations_CompanyLocationId",
            table: "Vacancies");

        migrationBuilder.DropTable(name: "CompanyLocations");

        migrationBuilder.DropIndex(
            name: "IX_Vacancies_CompanyLocationId",
            table: "Vacancies");

        migrationBuilder.DropColumn(
            name: "CompanyLocationId",
            table: "Vacancies");

        migrationBuilder.DropColumn(
            name: "LocationName",
            table: "Vacancies");

        migrationBuilder.DropColumn(
            name: "LogoDataUrl",
            table: "CompanyProfiles");
    }
}
