using GloryLikeBackend.Dtos.Vacancies;

namespace GloryLikeBackend.Services.Interfaces;

public interface IVacancyService
{
    Task<CreateVacancyResult> CreateAsync(
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default);
}

public enum CreateVacancyFailureKind
{
    None = 0,
    Validation = 1,
    Conflict = 2
}

public sealed class CreateVacancyResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int? VacancyId { get; private set; }
    public string PlatformVacancyId { get; private set; } = string.Empty;
    public DateTime? CreatedAtUtc { get; private set; }
    public CreateVacancyFailureKind FailureKind { get; private set; }

    public static CreateVacancyResult Created(
        int vacancyId,
        string platformVacancyId,
        DateTime createdAtUtc)
    {
        return new CreateVacancyResult
        {
            Success = true,
            Message = "Vacancy SQL-də uğurla yaradıldı.",
            VacancyId = vacancyId,
            PlatformVacancyId = platformVacancyId,
            CreatedAtUtc = createdAtUtc
        };
    }

    public static CreateVacancyResult Invalid(string message)
    {
        return Failed(
            message,
            CreateVacancyFailureKind.Validation);
    }

    public static CreateVacancyResult Conflict(string message)
    {
        return Failed(
            message,
            CreateVacancyFailureKind.Conflict);
    }

    private static CreateVacancyResult Failed(
        string message,
        CreateVacancyFailureKind failureKind)
    {
        return new CreateVacancyResult
        {
            Success = false,
            Message = message,
            FailureKind = failureKind
        };
    }
}
