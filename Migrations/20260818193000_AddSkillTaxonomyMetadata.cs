using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260818193000_AddSkillTaxonomyMetadata")]
public partial class AddSkillTaxonomyMetadata : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF COL_LENGTH(N'dbo.Skills', N'MinimumSenioritySortOrder') IS NULL
                ALTER TABLE dbo.Skills ADD MinimumSenioritySortOrder INT NOT NULL
                    CONSTRAINT DF_Skills_MinimumSenioritySortOrder DEFAULT (1);

            IF COL_LENGTH(N'dbo.Skills', N'IsCore') IS NULL
                ALTER TABLE dbo.Skills ADD IsCore BIT NOT NULL
                    CONSTRAINT DF_Skills_IsCore DEFAULT (0);

            IF COL_LENGTH(N'dbo.Skills', N'IsActive') IS NULL
                ALTER TABLE dbo.Skills ADD IsActive BIT NOT NULL
                    CONSTRAINT DF_Skills_IsActive DEFAULT (1);

            IF COL_LENGTH(N'dbo.Skills', N'AssessmentType') IS NULL
                ALTER TABLE dbo.Skills ADD AssessmentType NVARCHAR(10) NOT NULL
                    CONSTRAINT DF_Skills_AssessmentType DEFAULT (N'TP');

            IF COL_LENGTH(N'dbo.Skills', N'VerificationMethod') IS NULL
                ALTER TABLE dbo.Skills ADD VerificationMethod NVARCHAR(120) NOT NULL
                    CONSTRAINT DF_Skills_VerificationMethod DEFAULT (N'');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "VerificationMethod",
            table: "Skills");

        migrationBuilder.DropColumn(
            name: "AssessmentType",
            table: "Skills");

        migrationBuilder.DropColumn(
            name: "IsActive",
            table: "Skills");

        migrationBuilder.DropColumn(
            name: "IsCore",
            table: "Skills");

        migrationBuilder.DropColumn(
            name: "MinimumSenioritySortOrder",
            table: "Skills");
    }
}
