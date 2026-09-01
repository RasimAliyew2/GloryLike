using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260831120000_AddCompanyAccessRoles")]
public partial class AddCompanyAccessRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "Role",
            table: "CompanyTeamInvitations",
            type: "nvarchar(80)",
            maxLength: 80,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(40)",
            oldMaxLength: 40);

        migrationBuilder.CreateTable(
            name: "CompanyAccessRoles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerUserId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Scope = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                IsSystem = table.Column<bool>(type: "bit", nullable: false),
                IsFullAccess = table.Column<bool>(type: "bit", nullable: false),
                CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyAccessRoles", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyAccessRoles_Users_OwnerUserId",
                    column: item => item.OwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "CompanyAccessAuditEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OwnerUserId = table.Column<int>(type: "int", nullable: false),
                ActorUserId = table.Column<int>(type: "int", nullable: false),
                TargetUserId = table.Column<int>(type: "int", nullable: true),
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                EventType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Summary = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Details = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_CompanyAccessAuditEvents", item => item.Id));

        migrationBuilder.CreateTable(
            name: "CompanyAccessRolePermissions",
            columns: table => new
            {
                RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                PermissionKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyAccessRolePermissions", item => new { item.RoleId, item.PermissionKey });
                table.ForeignKey(
                    name: "FK_CompanyAccessRolePermissions_CompanyAccessRoles_RoleId",
                    column: item => item.RoleId,
                    principalTable: "CompanyAccessRoles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.AddColumn<Guid>(
            name: "AccessRoleId",
            table: "CompanyTeamInvitations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "UX_CompanyAccessRoles_Owner_Name",
            table: "CompanyAccessRoles",
            columns: new[] { "OwnerUserId", "Name" },
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_CompanyAccessAuditEvents_Owner_CreatedAt",
            table: "CompanyAccessAuditEvents",
            columns: new[] { "OwnerUserId", "CreatedAtUtc" });
        migrationBuilder.CreateIndex(
            name: "IX_CompanyAccessAuditEvents_ActorUserId",
            table: "CompanyAccessAuditEvents",
            column: "ActorUserId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyAccessAuditEvents_TargetUserId",
            table: "CompanyAccessAuditEvents",
            column: "TargetUserId");
        migrationBuilder.CreateIndex(
            name: "IX_CompanyTeamInvitations_AccessRoleId",
            table: "CompanyTeamInvitations",
            column: "AccessRoleId");

        migrationBuilder.AddForeignKey(
            name: "FK_CompanyTeamInvitations_CompanyAccessRoles_AccessRoleId",
            table: "CompanyTeamInvitations",
            column: "AccessRoleId",
            principalTable: "CompanyAccessRoles",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_CompanyTeamInvitations_CompanyAccessRoles_AccessRoleId",
            table: "CompanyTeamInvitations");
        migrationBuilder.DropIndex(
            name: "IX_CompanyTeamInvitations_AccessRoleId",
            table: "CompanyTeamInvitations");
        migrationBuilder.DropColumn(
            name: "AccessRoleId",
            table: "CompanyTeamInvitations");
        migrationBuilder.DropTable(name: "CompanyAccessAuditEvents");
        migrationBuilder.DropTable(name: "CompanyAccessRolePermissions");
        migrationBuilder.DropTable(name: "CompanyAccessRoles");
        migrationBuilder.AlterColumn<string>(
            name: "Role",
            table: "CompanyTeamInvitations",
            type: "nvarchar(40)",
            maxLength: 40,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "nvarchar(80)",
            oldMaxLength: 80);
    }
}
