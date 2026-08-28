using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

public partial class AddMicrosoftCalendarMeetings : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "MicrosoftCalendarConnections",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId = table.Column<int>(type: "int", nullable: false),
                MicrosoftUserId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ProtectedAccessToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ProtectedRefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                AccessTokenExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                GrantedScopes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                ConnectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_MicrosoftCalendarConnections", x => x.Id);
                table.ForeignKey(
                    name: "FK_MicrosoftCalendarConnections_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "InterviewMeetings",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                VacancyApplicationId = table.Column<int>(type: "int", nullable: false),
                OrganizerUserId = table.Column<int>(type: "int", nullable: false),
                Subject = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                CandidateEmail = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                StartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                EndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                IsOnlineMeeting = table.Column<bool>(type: "bit", nullable: false),
                GraphEventId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                WebLink = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                JoinUrl = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                TransactionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InterviewMeetings", x => x.Id);
                table.ForeignKey(
                    name: "FK_InterviewMeetings_Users_OrganizerUserId",
                    column: x => x.OrganizerUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_InterviewMeetings_VacancyApplications_VacancyApplicationId",
                    column: x => x.VacancyApplicationId,
                    principalTable: "VacancyApplications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "UX_MicrosoftCalendarConnections_UserId",
            table: "MicrosoftCalendarConnections",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_InterviewMeetings_Application_StartAtUtc",
            table: "InterviewMeetings",
            columns: new[] { "VacancyApplicationId", "StartAtUtc" });

        migrationBuilder.CreateIndex(
            name: "IX_InterviewMeetings_OrganizerUserId",
            table: "InterviewMeetings",
            column: "OrganizerUserId");

        migrationBuilder.CreateIndex(
            name: "UX_InterviewMeetings_TransactionId",
            table: "InterviewMeetings",
            column: "TransactionId",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InterviewMeetings");
        migrationBuilder.DropTable(name: "MicrosoftCalendarConnections");
    }
}
