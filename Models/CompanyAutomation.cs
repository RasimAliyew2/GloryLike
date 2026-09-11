namespace GloryLikeBackend.Models;

public sealed class CompanyAutomation
{
    public Guid Id { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RuleJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
public sealed class AutomationEmailDelivery
{
    public Guid Id { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public int VacancyId { get; set; }
    public int ApplicationId { get; set; }
    public int CandidateUserId { get; set; }
    public Guid RuleId { get; set; }
    public string EventKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string LastError { get; set; } = string.Empty;
    public int Attempts { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime NextAttemptAtUtc { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }
}
