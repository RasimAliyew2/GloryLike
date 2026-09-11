using System.ComponentModel.DataAnnotations;
namespace GloryLikeBackend.Dtos.CompanyTemplates;

public sealed class AutomationCondition
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "GreaterThan";
    public decimal Value { get; set; }
}
public sealed class AutomationRule
{
    public string EventType { get; set; } = string.Empty;
    public string? TargetStageName { get; set; } = string.Empty;
    public List<AutomationCondition> Conditions { get; set; } = [];
    public Guid LetterTemplateId { get; set; }
}
public sealed class SaveCompanyAutomationRequest
{
    public int ActorUserId { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public AutomationRule Rule { get; set; } = new();
}
public sealed class CompanyAutomationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public AutomationRule Rule { get; set; } = new();
    public string LetterName { get; set; } = string.Empty;
    public bool LetterAvailable { get; set; }
}
public sealed class VacancyAutomationCopy
{
    public Guid Id { get; set; }
    public Guid SourceTemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AutomationRule Rule { get; set; } = new();
    public string LetterName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
public sealed class AutomationDeliveryDto
{
    public Guid Id { get; set; }
    public int VacancyId { get; set; }
    public string RuleName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public string LastError { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
public sealed class CompanyAutomationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public bool CanManageTemplates { get; set; }
    public List<CompanyAutomationDto> Templates { get; set; } = [];
    public List<CompanyTemplateDto> Letters { get; set; } = [];
    public List<string> StageNames { get; set; } = [];
    public List<AutomationDeliveryDto> Deliveries { get; set; } = [];
}
