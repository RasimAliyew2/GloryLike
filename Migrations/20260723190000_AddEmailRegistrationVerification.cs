using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260723190000_AddEmailRegistrationVerification")]
public partial class AddEmailRegistrationVerification : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "UX_Users_PhoneNumber",
            table: "Users");

        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            table: "Users",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "nvarchar(30)",
            oldMaxLength: 30);

        migrationBuilder.AddColumn<string>(
            name: "AccountType",
            table: "Users",
            type: "nvarchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "candidate");

        migrationBuilder.AddColumn<string>(
            name: "CompanyName",
            table: "Users",
            type: "nvarchar(150)",
            maxLength: 150,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "CompanyType",
            table: "Users",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "EmailVerifiedAtUtc",
            table: "Users",
            type: "datetime2",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Industry",
            table: "Users",
            type: "nvarchar(120)",
            maxLength: 120,
            nullable: true);

        migrationBuilder.CreateTable(
            name: "PendingEmailRegistrations",
            columns: table => new
            {
                Id = table.Column<Guid>(
                    type: "uniqueidentifier",
                    nullable: false),
                Email = table.Column<string>(
                    type: "nvarchar(150)",
                    maxLength: 150,
                    nullable: false),
                PasswordHash = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: false),
                ProfileName = table.Column<string>(
                    type: "nvarchar(150)",
                    maxLength: 150,
                    nullable: false),
                AccountType = table.Column<string>(
                    type: "nvarchar(20)",
                    maxLength: 20,
                    nullable: false),
                CompanyType = table.Column<string>(
                    type: "nvarchar(30)",
                    maxLength: 30,
                    nullable: true),
                Industry = table.Column<string>(
                    type: "nvarchar(120)",
                    maxLength: 120,
                    nullable: true),
                VerificationCodeHash = table.Column<string>(
                    type: "nvarchar(500)",
                    maxLength: 500,
                    nullable: false),
                VerificationCodeExpiresAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                ResendAvailableAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                LastSentAtUtc = table.Column<DateTime>(
                    type: "datetime2",
                    nullable: false),
                FailedAttemptCount = table.Column<int>(
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
                    "PK_PendingEmailRegistrations",
                    item => item.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PendingEmailRegistrations_ExpiresAtUtc",
            table: "PendingEmailRegistrations",
            column: "VerificationCodeExpiresAtUtc");

        migrationBuilder.CreateIndex(
            name: "UX_PendingEmailRegistrations_Email",
            table: "PendingEmailRegistrations",
            column: "Email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "UX_Users_PhoneNumber",
            table: "Users",
            column: "PhoneNumber",
            unique: true,
            filter: "[PhoneNumber] IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "PendingEmailRegistrations");

        migrationBuilder.DropIndex(
            name: "UX_Users_PhoneNumber",
            table: "Users");

        migrationBuilder.Sql(
            """
            UPDATE [Users]
            SET [PhoneNumber] = CONCAT('+pending-', [Id])
            WHERE [PhoneNumber] IS NULL;
            """);

        migrationBuilder.AlterColumn<string>(
            name: "PhoneNumber",
            table: "Users",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: string.Empty,
            oldClrType: typeof(string),
            oldType: "nvarchar(30)",
            oldMaxLength: 30,
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "AccountType",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "CompanyName",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "CompanyType",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "EmailVerifiedAtUtc",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "Industry",
            table: "Users");

        migrationBuilder.CreateIndex(
            name: "UX_Users_PhoneNumber",
            table: "Users",
            column: "PhoneNumber",
            unique: true);
    }
}
