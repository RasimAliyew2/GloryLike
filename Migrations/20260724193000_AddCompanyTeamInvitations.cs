using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260724193000_AddCompanyTeamInvitations")]
public partial class AddCompanyTeamInvitations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "CompanyName",
            table: "PendingEmailRegistrations",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TeamInvitationId",
            table: "PendingEmailRegistrations",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "CompanyTeamInvitations",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                OwnerUserId = table.Column<int>(
                    type: "int",
                    nullable: false),
                AcceptedUserId = table.Column<int>(
                    type: "int",
                    nullable: true),
                Email = table.Column<string>(
                    type: "nvarchar(150)",
                    maxLength: 150,
                    nullable: false),
                Role = table.Column<string>(
                    type: "nvarchar(40)",
                    maxLength: 40,
                    nullable: false),
                Status = table.Column<string>(
                    type: "nvarchar(20)",
                    maxLength: 20,
                    nullable: false),
                TokenHash = table.Column<string>(
                    type: "nvarchar(64)",
                    maxLength: 64,
                    nullable: false),
                ExpiresAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                SentAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                CreatedAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                AcceptedAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey(
                    "PK_CompanyTeamInvitations",
                    item => item.Id);

                table.ForeignKey(
                    name: "FK_CompanyTeamInvitations_Users_AcceptedUserId",
                    column: item => item.AcceptedUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                table.ForeignKey(
                    name: "FK_CompanyTeamInvitations_Users_OwnerUserId",
                    column: item => item.OwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CompanyTeamInvitations_AcceptedUserId",
            table: "CompanyTeamInvitations",
            column: "AcceptedUserId");

        migrationBuilder.CreateIndex(
            name: "UX_CompanyTeamInvitations_Owner_Email",
            table: "CompanyTeamInvitations",
            columns: new[]
            {
                "OwnerUserId",
                "Email"
            },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_CompanyTeamInvitations_TokenHash",
            table: "CompanyTeamInvitations",
            column: "TokenHash",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PendingEmailRegistrations_TeamInvitationId",
            table: "PendingEmailRegistrations",
            column: "TeamInvitationId");

        migrationBuilder.AddForeignKey(
            name: "FK_PendingEmailRegistrations_CompanyTeamInvitations_TeamInvitationId",
            table: "PendingEmailRegistrations",
            column: "TeamInvitationId",
            principalTable: "CompanyTeamInvitations",
            principalColumn: "Id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_PendingEmailRegistrations_CompanyTeamInvitations_TeamInvitationId",
            table: "PendingEmailRegistrations");

        migrationBuilder.DropIndex(
            name: "IX_PendingEmailRegistrations_TeamInvitationId",
            table: "PendingEmailRegistrations");

        migrationBuilder.DropColumn(
            name: "CompanyName",
            table: "PendingEmailRegistrations");

        migrationBuilder.DropColumn(
            name: "TeamInvitationId",
            table: "PendingEmailRegistrations");

        migrationBuilder.DropTable(
            name: "CompanyTeamInvitations");
    }
}
