namespace GloryLikeBackend.Dtos.Reports;

public sealed class OrganizationReportsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public List<OrganizationReportCategoryDto> Categories { get; set; } = [];
}

public sealed class OrganizationReportCategoryDto
{
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<OrganizationReportMetricDto> Metrics { get; set; } = [];
}

public sealed class OrganizationReportMetricDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Tone { get; set; } = "neutral";
}
