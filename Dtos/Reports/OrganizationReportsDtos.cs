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

public sealed class OrganizationAnalyticsDashboardResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public bool ContainsDemoData { get; set; }
    public int TotalApplications { get; set; }
    public int HiredCount { get; set; }
    public bool HiredCountIsDemo { get; set; }
    public int InProcessApplications { get; set; }
    public int AverageTimeToHireDays { get; set; }
    public bool AverageTimeToHireIsDemo { get; set; }
    public decimal AcceptedOfferRatePercent { get; set; }
    public bool AcceptedOfferRateIsDemo { get; set; }
    public int ActiveVacancies { get; set; }
    public List<ReportsMonthlyActivityDto> MonthlyActivity { get; set; } = [];
    public List<ReportsFunnelStageDto> FunnelStages { get; set; } = [];
    public List<ReportsSourceDto> Sources { get; set; } = [];
    public List<ReportsTeamMemberDto> TeamMembers { get; set; } = [];
    public List<ReportsVacancyTimingDto> VacancyTimings { get; set; } = [];
}

public sealed class ReportsMonthlyActivityDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Applications { get; set; }
    public int Hired { get; set; }
    public bool HiredIsDemo { get; set; }
}

public sealed class ReportsFunnelStageDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public bool IsDemo { get; set; }
}

public sealed class ReportsSourceDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public bool IsDemo { get; set; }
}

public sealed class ReportsTeamMemberDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public int VacancyCount { get; set; }
    public int ApplicationCount { get; set; }
    public int HiredCount { get; set; }
    public bool HiredCountIsDemo { get; set; }
}

public sealed class ReportsVacancyTimingDto
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public int DaysOpen { get; set; }
    public int TimeToHireDays { get; set; }
    public bool TimeToHireIsDemo { get; set; }
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
