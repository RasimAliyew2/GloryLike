using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260823150000_AddCompanyCandidateMessages")]
public partial class AddCompanyCandidateMessages : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CompanyCandidateMessages",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CompanyOwnerUserId = table.Column<int>(type: "int", nullable: false),
                SenderUserId = table.Column<int>(type: "int", nullable: false),
                RecipientUserId = table.Column<int>(type: "int", nullable: false),
                CandidateUserId = table.Column<int>(type: "int", nullable: false),
                Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CompanyCandidateMessages", item => item.Id);
                table.ForeignKey(
                    name: "FK_CompanyCandidateMessages_Users_CandidateUserId",
                    column: item => item.CandidateUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyCandidateMessages_Users_CompanyOwnerUserId",
                    column: item => item.CompanyOwnerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyCandidateMessages_Users_RecipientUserId",
                    column: item => item.RecipientUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_CompanyCandidateMessages_Users_SenderUserId",
                    column: item => item.SenderUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CompanyCandidateMessages_CandidateUserId",
            table: "CompanyCandidateMessages",
            column: "CandidateUserId");

        migrationBuilder.CreateIndex(
            name: "IX_CompanyCandidateMessages_CompanyOwnerUserId",
            table: "CompanyCandidateMessages",
            column: "CompanyOwnerUserId");

        migrationBuilder.CreateIndex(
            name: "IX_CompanyCandidateMessages_RecipientUserId",
            table: "CompanyCandidateMessages",
            column: "RecipientUserId");

        migrationBuilder.CreateIndex(
            name: "IX_CompanyCandidateMessages_SenderUserId",
            table: "CompanyCandidateMessages",
            column: "SenderUserId");

        migrationBuilder.CreateIndex(
            name: "IX_CompanyCandidateMessages_CandidateThread",
            table: "CompanyCandidateMessages",
            columns: new[] { "CompanyOwnerUserId", "CandidateUserId", "CreatedAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_CompanyCandidateMessages_RecipientUnread",
            table: "CompanyCandidateMessages",
            columns: new[] { "CompanyOwnerUserId", "RecipientUserId", "ReadAtUtc" });

        migrationBuilder.AddCheckConstraint(
            name: "CK_CompanyCandidateMessages_DifferentUsers",
            table: "CompanyCandidateMessages",
            sql: "[SenderUserId] <> [RecipientUserId]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "CompanyCandidateMessages");
    }
}
