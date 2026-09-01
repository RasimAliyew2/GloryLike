namespace GloryLikeBackend.Dtos.Vacancies;

public sealed class CandidateApplicationListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public List<CandidateApplicationListItemDto> Applications { get; set; } = [];
}

public sealed class CandidateApplicationListItemDto
{
    public int ApplicationId { get; set; }
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string VacancyStatus { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;
    public string FunnelStageName { get; set; } = string.Empty;
    public int FunnelStageIndex { get; set; }
    public int FunnelStageCount { get; set; }
    public DateTime AppliedAtUtc { get; set; }
    public DateTime? FunnelStageUpdatedAtUtc { get; set; }
    public DateTime? HiredAtUtc { get; set; }
}

public sealed class CandidateNotificationListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public int UnreadCount { get; set; }
    public List<CandidateNotificationItemDto> Notifications { get; set; } = [];
}

public sealed class CandidateNotificationItemDto
{
    public long NotificationId { get; set; }
    public int VacancyId { get; set; }
    public int ApplicationId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public sealed class MarkCandidateNotificationReadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long NotificationId { get; set; }
    public int VacancyId { get; set; }
    public int ApplicationId { get; set; }
    public bool WasAlreadyRead { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}
