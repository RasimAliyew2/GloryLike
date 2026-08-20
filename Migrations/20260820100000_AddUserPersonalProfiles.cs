using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260820100000_AddUserPersonalProfiles")]
public partial class AddUserPersonalProfiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "About",
            table: "Users",
            type: "nvarchar(1000)",
            maxLength: 1000,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "BirthDate",
            table: "Users",
            type: "date",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfileImageDataUrl",
            table: "Users",
            type: "nvarchar(max)",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "About", table: "Users");
        migrationBuilder.DropColumn(name: "BirthDate", table: "Users");
        migrationBuilder.DropColumn(name: "ProfileImageDataUrl", table: "Users");
    }
}
