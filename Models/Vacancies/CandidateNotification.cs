namespace GloryLikeBackend.Models.Vacancies;

public static class CandidateNotificationTypes
{
    public const string FunnelStageAdvanced = "FunnelStageAdvanced";
}

public sealed class CandidateNotification
{
    public long Id { get; set; }
    public int CandidateUserId { get; set; }
    public int VacancyId { get; set; }
    public int VacancyApplicationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
