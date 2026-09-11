using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace GloryLikeBackend.Services;

public sealed class CompanyAutomationService(AppDbContext db, ICompanyAccessService accessService,
    ICompanyTemplateService letters, ICompanyFunnelService funnels)
{
    public async Task<CompanyAutomationResponse> GetAsync(int actor, CancellationToken ct)
    {
        var access = await accessService.ResolveAsync(actor, ct);
        if (access is null) return Fail("Company access is required.", "forbidden");
        var letterResult = await letters.GetAsync(actor, ct);
        var candidateLetters = letterResult.Templates.Where(l => l.Audience == "Candidate").ToList();
        var rows = await db.CompanyAutomations.AsNoTracking().Where(r => r.CompanyOwnerUserId == access.CompanyOwnerUserId).OrderBy(r => r.CreatedAtUtc).ToListAsync(ct);
        var funnelResult = await funnels.GetAsync(actor, ct);
        var stageNames = await db.VacancyFunnelStages.AsNoTracking()
            .Where(s => s.Vacancy.CompanyOwnerUserId == access.CompanyOwnerUserId).Select(s => s.StageName).Distinct().ToListAsync(ct);
        var deliveries = await db.AutomationEmailDeliveries.AsNoTracking().Where(d => d.CompanyOwnerUserId == access.CompanyOwnerUserId)
            .OrderByDescending(d => d.CreatedAtUtc).Take(100).Select(d => new AutomationDeliveryDto {
                Id=d.Id, VacancyId=d.VacancyId, RuleName=d.RuleName, RecipientEmail=d.RecipientEmail,
                EventType=d.EventType, Status=d.Status, Attempts=d.Attempts, LastError=d.LastError,
                CreatedAtUtc=d.CreatedAtUtc, SentAtUtc=d.SentAtUtc }).ToListAsync(ct);
        return new() { Success=true, CanManageTemplates=access.CanManageTemplates, Letters=candidateLetters,
            StageNames=stageNames.Concat(funnelResult.Templates.SelectMany(f => f.Stages.Select(s => s.StageName))).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList(),
            Templates=rows.Select(r => { var rule=JsonSerializer.Deserialize<AutomationRule>(r.RuleJson)!;
                var letter=candidateLetters.FirstOrDefault(l => l.Id==rule.LetterTemplateId);
                return new CompanyAutomationDto { Id=r.Id, Name=r.Name, IsEnabled=r.IsEnabled, Rule=rule,
                    LetterName=letter?.Name ?? "Letter unavailable", LetterAvailable=letter is not null }; }).ToList(),
            Deliveries=access.CanManageTemplates ? deliveries : [] };
    }
    public async Task<CompanyAutomationResponse> SaveAsync(Guid? id, SaveCompanyAutomationRequest request, CancellationToken ct)
    {
        var access=await accessService.ResolveAsync(request.ActorUserId, ct);
        if (access?.CanManageTemplates != true) return Fail("Template management permission is required.", "forbidden");
        request.Name=request.Name?.Trim() ?? "";
        if (request.Name.Length is < 1 or > 120) return Fail("Name must contain 1–120 characters.");
        var validation=AutomationRuleEvaluator.Validate(request.Rule);
        if (validation.Length>0) return Fail(validation);
        var library=await GetAsync(request.ActorUserId, ct);
        if (!library.Letters.Any(l => l.Id==request.Rule.LetterTemplateId)) return Fail("Select an available candidate letter from your company's Letters library.");
        var selectedLetter=library.Letters.Single(l => l.Id==request.Rule.LetterTemplateId);
        var letterError=AutomationEngine.ValidateLetter(selectedLetter.Subject,selectedLetter.Body);
        if (letterError.Length>0) return Fail(letterError);
        if (request.Rule.EventType=="StageAdvanced" && !library.StageNames.Contains(request.Rule.TargetStageName!.Trim(), StringComparer.OrdinalIgnoreCase))
            return Fail("Select a stage from your company's funnels or vacancies.");
        var row=id.HasValue ? await db.CompanyAutomations.FirstOrDefaultAsync(r => r.Id==id && r.CompanyOwnerUserId==access.CompanyOwnerUserId, ct) : null;
        if (id.HasValue && row is null) return Fail("Automation not found for this company.", "not_found");
        if (row is null) { row=new() { Id=Guid.NewGuid(), CompanyOwnerUserId=access.CompanyOwnerUserId, CreatedByUserId=request.ActorUserId, CreatedAtUtc=DateTime.UtcNow }; db.CompanyAutomations.Add(row); }
        request.Rule.TargetStageName=request.Rule.TargetStageName?.Trim() ?? "";
        row.Name=request.Name; row.IsEnabled=request.IsEnabled; row.RuleJson=JsonSerializer.Serialize(request.Rule); row.UpdatedAtUtc=DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new() { Success=true, Message="Automation template saved." };
    }
    public async Task<CompanyAutomationResponse> DeleteAsync(int actor, Guid id, CancellationToken ct)
    {
        var access=await accessService.ResolveAsync(actor, ct);
        if (access?.CanManageTemplates != true) return Fail("Template management permission is required.", "forbidden");
        var row=await db.CompanyAutomations.FirstOrDefaultAsync(r => r.Id==id && r.CompanyOwnerUserId==access.CompanyOwnerUserId, ct);
        if (row is null) return Fail("Automation not found for this company.", "not_found");
        db.CompanyAutomations.Remove(row); await db.SaveChangesAsync(ct);
        return new() { Success=true, Message="Template deleted. Existing vacancy copies are unchanged." };
    }
    public async Task<CompanyAutomationResponse> RetryAsync(int actor, Guid id, CancellationToken ct)
    {
        var access=await accessService.ResolveAsync(actor, ct);
        if (access?.CanManageTemplates != true) return Fail("Template management permission is required.", "forbidden");
        var count=await db.AutomationEmailDeliveries.Where(d => d.Id==id && d.CompanyOwnerUserId==access.CompanyOwnerUserId && (d.Status=="Failed" || d.Status=="Uncertain"))
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.Status,"Pending").SetProperty(d => d.Attempts,0)
                .SetProperty(d => d.NextAttemptAtUtc,DateTime.UtcNow).SetProperty(d => d.LastError,""), ct);
        return count==1 ? new() { Success=true, Message="Email queued for retry." } : Fail("This delivery cannot be retried.");
    }
    public static CompanyAutomationResponse Fail(string message, string code="validation") => new() { Message=message, ErrorCode=code };
}
