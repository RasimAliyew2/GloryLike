namespace GloryLikeBackend.Dtos.Vacancies;

public sealed class EmployerVacancyListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EmployerUserId { get; set; }
    public List<EmployerVacancyListItemDto> Vacancies { get; set; } = new();
}

public sealed class EmployerVacancyListItemDto
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int CandidateCount { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
