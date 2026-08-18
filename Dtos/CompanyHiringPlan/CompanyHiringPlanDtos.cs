using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.CompanyHiringPlan;

public sealed class SaveCompanyHiringPlanRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    [Range(1, int.MaxValue)]
    public int JobFamilyId { get; set; }

    [Range(1, int.MaxValue)]
    public int PositionId { get; set; }

    [Range(1, int.MaxValue)]
    public int SeniorityId { get; set; }

    [Range(1, 1000)]
    public int Headcount { get; set; } = 1;

    [Required]
    [RegularExpression("^(Critical|High|Medium|Low)$")]
    public string Priority { get; set; } = "Medium";

    public DateTime? TargetStartDate { get; set; }

    [Required]
    [RegularExpression("^(Full-time|Part-time|Contract|Temporary|Internship)$")]
    public string EmploymentType { get; set; } = "Full-time";

    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

public sealed class CompanyHiringPlanResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public CompanyHiringPlanDto? Plan { get; set; }
    public List<CompanyHiringPlanDto> Plans { get; set; } = new();
}

public sealed class CompanyHiringPlanDto
{
    public int Id { get; set; }
    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;
    public int SeniorityId { get; set; }
    public string SeniorityName { get; set; } = string.Empty;
    public int Headcount { get; set; }
    public string Priority { get; set; } = string.Empty;
    public DateTime? TargetStartDate { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int VacancyCount { get; set; }
    public int FinishedVacancyCount { get; set; }
    public int RemainingVacancyCount { get; set; }
    public bool CanCreateVacancy { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<CompanyHiringPlanVacancyDto> Vacancies { get; set; } = new();
}

public sealed class CompanyHiringPlanVacancyDto
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public static class CompanyHiringPlanStatuses
{
    public const string Planned = "Planned";
    public const string InProgress = "In Progress";
    public const string Finished = "Finished";
}

public static class CompanyHiringPlanErrorCodes
{
    public const string Validation = "validation";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
}
