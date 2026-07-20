namespace GloryLikeBackend.Dtos.Vacancies;

public sealed class CreateVacancyResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public DateTime? CreatedAtUtc { get; set; }
}
