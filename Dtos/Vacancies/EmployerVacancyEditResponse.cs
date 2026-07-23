namespace GloryLikeBackend.Dtos.Vacancies;

public sealed class EmployerVacancyEditResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int EmployerUserId { get; set; }
    public int VacancyId { get; set; }
    public string Status { get; set; } = string.Empty;
    public CreateVacancyPayload? Vacancy { get; set; }
}

public sealed class UpdateVacancyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int VacancyId { get; set; }
    public int EmployerUserId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
}
