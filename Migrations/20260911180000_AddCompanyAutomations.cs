using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
#nullable disable
namespace GloryLikeBackend.Migrations;
[DbContext(typeof(AppDbContext))]
[Migration("20260911180000_AddCompanyAutomations")]
public sealed class AddCompanyAutomations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name:"AutomationsJson",table:"Vacancies",type:"nvarchar(max)",nullable:false,defaultValue:"[]");
        migrationBuilder.AddColumn<int>(name:"AutomationEventVersion",table:"Vacancies",type:"int",nullable:false,defaultValue:0);
        migrationBuilder.AddColumn<int>(name:"AutomationEventVersion",table:"VacancyApplications",type:"int",nullable:false,defaultValue:0);
        migrationBuilder.CreateTable(name:"CompanyAutomations", columns:table => new
        {
            Id=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            CompanyOwnerUserId=table.Column<int>(type:"int",nullable:false),
            CreatedByUserId=table.Column<int>(type:"int",nullable:false),
            Name=table.Column<string>(type:"nvarchar(120)",nullable:false,maxLength:120),
            RuleJson=table.Column<string>(type:"nvarchar(max)",nullable:false),
            IsEnabled=table.Column<bool>(type:"bit",nullable:false),
            CreatedAtUtc=table.Column<DateTime>(type:"datetime2",nullable:false),
            UpdatedAtUtc=table.Column<DateTime>(type:"datetime2",nullable:false),
        },constraints:table => table.PrimaryKey("PK_CompanyAutomations",x => x.Id));
        migrationBuilder.CreateIndex(name:"IX_CompanyAutomations_CompanyOwnerUserId",table:"CompanyAutomations",columns:new[]{"CompanyOwnerUserId"},unique:false);
        migrationBuilder.CreateTable(name:"AutomationEmailDeliveries", columns:table => new
        {
            Id=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            CompanyOwnerUserId=table.Column<int>(type:"int",nullable:false),
            VacancyId=table.Column<int>(type:"int",nullable:false),
            ApplicationId=table.Column<int>(type:"int",nullable:false),
            CandidateUserId=table.Column<int>(type:"int",nullable:false),
            RuleId=table.Column<Guid>(type:"uniqueidentifier",nullable:false),
            EventKey=table.Column<string>(type:"nvarchar(128)",nullable:false,maxLength:128),
            EventType=table.Column<string>(type:"nvarchar(32)",nullable:false,maxLength:32),
            RuleName=table.Column<string>(type:"nvarchar(120)",nullable:false,maxLength:120),
            RecipientEmail=table.Column<string>(type:"nvarchar(320)",nullable:false,maxLength:320),
            Subject=table.Column<string>(type:"nvarchar(max)",nullable:false),
            Body=table.Column<string>(type:"nvarchar(max)",nullable:false),
            Status=table.Column<string>(type:"nvarchar(16)",nullable:false,maxLength:16),
            LastError=table.Column<string>(type:"nvarchar(1000)",nullable:false,maxLength:1000),
            Attempts=table.Column<int>(type:"int",nullable:false),
            CreatedAtUtc=table.Column<DateTime>(type:"datetime2",nullable:false),
            NextAttemptAtUtc=table.Column<DateTime>(type:"datetime2",nullable:false),
            LeaseUntilUtc=table.Column<DateTime>(type:"datetime2",nullable:true),
            SentAtUtc=table.Column<DateTime>(type:"datetime2",nullable:true),
        },constraints:table => table.PrimaryKey("PK_AutomationEmailDeliveries",x => x.Id));
        migrationBuilder.CreateIndex(name:"IX_AutomationEmailDeliveries_CompanyOwnerUserId",table:"AutomationEmailDeliveries",columns:new[]{"CompanyOwnerUserId"},unique:false);
        migrationBuilder.CreateIndex(name:"IX_AutomationEmailDeliveries_EventKey_RuleId_ApplicationId",table:"AutomationEmailDeliveries",columns:new[]{"EventKey", "RuleId", "ApplicationId"},unique:true);
        migrationBuilder.CreateIndex(name:"IX_AutomationEmailDeliveries_Status_NextAttemptAtUtc",table:"AutomationEmailDeliveries",columns:new[]{"Status", "NextAttemptAtUtc"},unique:false);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AutomationEmailDeliveries");
        migrationBuilder.DropTable("CompanyAutomations");
        migrationBuilder.DropColumn("AutomationsJson","Vacancies");
        migrationBuilder.DropColumn("AutomationEventVersion","Vacancies");
        migrationBuilder.DropColumn("AutomationEventVersion","VacancyApplications");
    }
}
