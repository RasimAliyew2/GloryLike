namespace GloryLikeBackend.Dtos.Vacancies;

public sealed class CandidateVacancyListResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public List<int> CandidateJobFamilyIds { get; set; } = new();
    public List<string> CandidateJobFamilyNames { get; set; } = new();
    public List<CandidateVacancyListItemDto> Vacancies { get; set; } = new();
}

public sealed class CandidateVacancyListItemDto
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public int EmployerUserId { get; set; }
    public string EmployerName { get; set; } = string.Empty;
    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string SeniorityName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool HideSalary { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int MatchScore { get; set; }
    public List<CandidateVacancySkillDto> Skills { get; set; } = new();
    public bool HasApplied { get; set; }
    public int? ApplicationId { get; set; }
    public string ApplicationStatus { get; set; } = string.Empty;
    public DateTime? AppliedAtUtc { get; set; }
}

public sealed class CandidateVacancySkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Weight { get; set; }
    public string RequirementType { get; set; } = string.Empty;
    public bool IsMatched { get; set; }
}

public sealed class ApplyToVacancyRequest
{
    public int CandidateUserId { get; set; }
}

public sealed class ApplyToVacancyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int VacancyId { get; set; }
    public int CandidateUserId { get; set; }
    public int? ApplicationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? AppliedAtUtc { get; set; }
    public bool AlreadyApplied { get; set; }
}
