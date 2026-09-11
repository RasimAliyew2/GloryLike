using System.Data;
using System.Data.Common;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Vacancies;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace GloryLikeBackend.Services;

public sealed class AutomationEngine(AppDbContext db, ICompanyTemplateService letterService,
    ILogger<AutomationEngine> logger)
{
    public static List<VacancyAutomationCopy> ReadCopies(string json) =>
        JsonSerializer.Deserialize<List<VacancyAutomationCopy>>(json) ?? [];

    public async Task<(string Json, string? Error)> BindAsync(int actor, int owner, List<Guid>? ids,
        string existingJson, IEnumerable<string> stageNames, CancellationToken ct)
    {
        if (ids is null) return (existingJson, null); // Older clients preserve existing rules.
        if (ids.Count>20 || ids.Distinct().Count()!=ids.Count) return ("[]", "Select at most 20 distinct automations.");
        var existing=ReadCopies(existingJson);
        var availableStages=stageNames.Select(s => s.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rows=await db.CompanyAutomations.AsNoTracking().Where(r => r.CompanyOwnerUserId==owner && ids.Contains(r.Id)).ToListAsync(ct);
        var letters=await letterService.GetAsync(actor, ct);
        var copies=new List<VacancyAutomationCopy>();
        foreach (var id in ids)
        {
            var copy=existing.FirstOrDefault(c => c.SourceTemplateId==id);
            if (copy is null)
            {
                var row=rows.FirstOrDefault(r => r.Id==id && r.IsEnabled);
                if (row is null) return ("[]", "A selected automation is unavailable for this company.");
                var rule=JsonSerializer.Deserialize<AutomationRule>(row.RuleJson)!;
                var letter=letters.Templates.FirstOrDefault(l => l.Id==rule.LetterTemplateId && l.Audience=="Candidate");
                if (letter is null) return ("[]", $"Select a valid candidate letter for {row.Name} before using it.");
                var tokensError=ValidateLetter(letter.Subject,letter.Body);
                if (tokensError.Length>0) return ("[]", tokensError);
                copy=new() { Id=Guid.NewGuid(),SourceTemplateId=id,Name=row.Name,Rule=rule,
                    LetterName=letter.Name,Subject=letter.Subject,Body=letter.Body };
            }
            var ruleError=AutomationRuleEvaluator.Validate(copy.Rule);
            if (ruleError.Length>0) return ("[]", ruleError);
            if (copy.Rule.EventType=="StageAdvanced" && !availableStages.Contains(copy.Rule.TargetStageName?.Trim() ?? ""))
                return ("[]", $"Automation '{copy.Name}' requires funnel stage '{copy.Rule.TargetStageName}'. Add that stage or deselect this automation.");
            copies.Add(copy);
        }
        return (JsonSerializer.Serialize(copies),null);
    }
    public static string ValidateLetter(string subject,string body)
    {
        var allowed=new[] { "candidate_name","company_name","vacancy_title","recruiter_name","vacancy_link" };
        var unsupported=Regex.Matches(subject+"\n"+body,@"\{([a-z_]+)\}").Select(m => m.Groups[1].Value).FirstOrDefault(token => !allowed.Contains(token));
        return unsupported is null ? "" : $"Candidate automation emails cannot resolve {{{unsupported}}}. Use candidate_name, company_name, vacancy_title, recruiter_name or vacancy_link in Letters.";
    }
    // Called before the same SaveChanges as the domain transition: no lost event/email gap.
    public async Task EnqueueAsync(Vacancy vacancy, int actor, string eventType, string stageName,
        string eventKey, IReadOnlyCollection<int>? applicationIds, CancellationToken ct)
    {
        var rules=ReadCopies(vacancy.AutomationsJson).Where(r => r.Rule.EventType==eventType &&
            (eventType!="StageAdvanced" || string.Equals(r.Rule.TargetStageName,stageName,StringComparison.OrdinalIgnoreCase))).ToList();
        if (rules.Count==0) return;
        var applicationQuery=db.VacancyApplications.AsNoTracking().Where(a => a.VacancyId==vacancy.Id);
        if (applicationIds is not null) applicationQuery=applicationQuery.Where(a => applicationIds.Contains(a.Id));
        var applications=await applicationQuery.ToListAsync(ct);
        var company=await db.CompanyProfiles.AsNoTracking().Where(c => c.OwnerUserId==vacancy.CompanyOwnerUserId).Select(c => c.CompanyName).FirstOrDefaultAsync(ct);
        var recruiter=await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id==actor,ct);
        await db.Entry(vacancy).Collection(v => v.SkillRequirements).LoadAsync(ct);
        foreach (var application in applications)
        {
            var candidate=await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id==application.CandidateUserId,ct);
            if (candidate is null) continue;
            var skills=await db.UserSkills.AsNoTracking().Where(s => s.UserId==candidate.Id).ToListAsync(ct);
            var metrics=new Dictionary<string,decimal?> {
                ["Age"]=AutomationRuleEvaluator.Age(candidate.BirthDate,candidate.Age,DateTime.UtcNow),
                ["Score"]=VacancyService.AutomationScore(skills),
                ["MatchScore"]=VacancyService.AutomationMatchScore(vacancy,skills),
                ["ExperienceYears"]=null };
            if (rules.Any(r => r.Rule.Conditions.Any(c => c.Field=="ExperienceYears")))
                metrics["ExperienceYears"]=await ReadExperienceAsync(candidate.Id,ct);
            foreach (var rule in rules)
            {
                var matches=AutomationRuleEvaluator.Matches(rule.Rule,eventType,stageName,metrics);
                var now=DateTime.UtcNow;
                var delivery=new AutomationEmailDelivery { Id=Guid.NewGuid(),CompanyOwnerUserId=vacancy.CompanyOwnerUserId,
                    VacancyId=vacancy.Id,ApplicationId=application.Id,CandidateUserId=candidate.Id,RuleId=rule.Id,
                    EventKey=eventKey,EventType=eventType,RuleName=rule.Name,RecipientEmail=candidate.Email,
                    Status=matches ? "Pending":"Skipped",CreatedAtUtc=now,NextAttemptAtUtc=now,
                    LastError=matches ? "":"Candidate conditions did not match or required profile data is missing." };
                var values=new Dictionary<string,string> {
                    ["candidate_name"]=DisplayName(candidate), ["company_name"]=company ?? recruiter?.CompanyName ?? "Hiring team",
                    ["vacancy_title"]=string.IsNullOrWhiteSpace(vacancy.RoleTitle)?vacancy.PositionName:vacancy.RoleTitle,
                    ["recruiter_name"]=recruiter is null ? "Recruitment team":DisplayName(recruiter),
                    ["vacancy_link"]=$"https://bothfind.com/Applications/{vacancy.Id}" };
                delivery.Subject=Render(rule.Subject,values).Replace("\r"," ").Replace("\n"," ");
                delivery.Body=Render(rule.Body,values);
                if (matches && (!MailAddress.TryCreate(candidate.Email,out _) || string.IsNullOrWhiteSpace(delivery.Subject) || ValidateLetter(rule.Subject,rule.Body).Length>0))
                { delivery.Status="Failed";delivery.LastError="Candidate email address or selected letter is invalid."; }
                db.AutomationEmailDeliveries.Add(delivery);
            }
        }
    }
    private static string DisplayName(User user) => string.Join(" ",new[]{user.Name,user.Surname}.Where(s => !string.IsNullOrWhiteSpace(s)));
    private static string Render(string text,IReadOnlyDictionary<string,string> values) =>
        Regex.Replace(text,@"\{([a-z_]+)\}",m => values.TryGetValue(m.Groups[1].Value,out var value)?value:m.Value);
    private async Task<decimal?> ReadExperienceAsync(int userId,CancellationToken ct)
    {
        var connection=db.Database.GetDbConnection();var opened=connection.State!=ConnectionState.Open;
        try
        {
            if(opened) await connection.OpenAsync(ct);
            await using var command=connection.CreateCommand();
            command.CommandText="SELECT StartYear, EndYear FROM dbo.UserWorkExperiences WHERE UserId=@userId";
            var parameter=command.CreateParameter();parameter.ParameterName="@userId";parameter.Value=userId;command.Parameters.Add(parameter);
            var rows=new List<(string,string)>();
            await using var reader=await command.ExecuteReaderAsync(ct);
            while(await reader.ReadAsync(ct)) rows.Add((reader.IsDBNull(0)?"":reader.GetString(0),reader.IsDBNull(1)?"":reader.GetString(1)));
            return AutomationRuleEvaluator.ExperienceYears(rows,DateTime.UtcNow);
        }
        catch(DbException ex) { logger.LogWarning(ex,"Automation experience data unavailable for candidate {UserId}.",userId);return null; }
        finally { if(opened && connection.State==ConnectionState.Open) await connection.CloseAsync(); }
    }
}
