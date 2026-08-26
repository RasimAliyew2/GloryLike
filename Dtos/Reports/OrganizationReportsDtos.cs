namespace GloryLikeBackend.Dtos.Reports;

public sealed class OrganizationReportCatalogResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public List<OrganizationReportDefinitionDto> Reports { get; set; } = [];
}

public sealed class OrganizationReportDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class VacancyCreationReportResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public int TotalVacancyCount { get; set; }
    public List<VacancyCreatorReportRowDto> Employees { get; set; } = [];
}

public sealed class VacancyCreatorReportRowDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public int VacancyCount { get; set; }
    public List<DateTime> VacancyCreationDatesUtc { get; set; } = [];
    public List<VacancyCreationReportItemDto> Vacancies { get; set; } = [];
}

public sealed class VacancyCreationReportItemDto
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ReportEmployeeProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string About { get; set; } = string.Empty;
    public string ProfileImageDataUrl { get; set; } = string.Empty;
    public int CreatedVacancyCount { get; set; }
}
