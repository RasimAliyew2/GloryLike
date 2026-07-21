using GloryLikeBackend.Dtos.Vacancies;
using GloryLikeBackend.Models.Vacancies;

namespace GloryLikeBackend.Services.Interfaces;

public interface IVacancyService
{
    Task<CandidateVacancyListResponse?> GetCandidateVacanciesAsync(
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<List<EmployerVacancyListItemDto>> GetEmployerVacanciesAsync(
        int employerUserId,
        CancellationToken cancellationToken = default);

    Task<ApplyToVacancyResult> ApplyToVacancyAsync(
        int vacancyId,
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<CreateVacancyResult> CreateAsync(
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default);
}

public enum ApplyToVacancyFailureKind
{
    None = 0,
    Validation = 1,
    NotFound = 2
}

public sealed class ApplyToVacancyResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int VacancyId { get; private set; }
    public int CandidateUserId { get; private set; }
    public int? ApplicationId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime? AppliedAtUtc { get; private set; }
    public bool AlreadyApplied { get; private set; }
    public ApplyToVacancyFailureKind FailureKind { get; private set; }

    public static ApplyToVacancyResult Applied(
        VacancyApplication application,
        bool alreadyApplied)
    {
        return new ApplyToVacancyResult
        {
            Success = true,
            Message = alreadyApplied
                ? "Bu vacancy üçün müraciət artıq mövcuddur."
                : "Müraciət SQL-də saxlanıldı.",
            VacancyId = application.VacancyId,
            CandidateUserId = application.CandidateUserId,
            ApplicationId = application.Id,
            Status = application.Status,
            AppliedAtUtc = application.AppliedAtUtc,
            AlreadyApplied = alreadyApplied
        };
    }

    public static ApplyToVacancyResult Invalid(
        int vacancyId,
        int candidateUserId,
        string message)
    {
        return Failed(
            vacancyId,
            candidateUserId,
            message,
            ApplyToVacancyFailureKind.Validation);
    }

    public static ApplyToVacancyResult NotFound(
        int vacancyId,
        int candidateUserId,
        string message)
    {
        return Failed(
            vacancyId,
            candidateUserId,
            message,
            ApplyToVacancyFailureKind.NotFound);
    }

    private static ApplyToVacancyResult Failed(
        int vacancyId,
        int candidateUserId,
        string message,
        ApplyToVacancyFailureKind failureKind)
    {
        return new ApplyToVacancyResult
        {
            Success = false,
            Message = message,
            VacancyId = vacancyId,
            CandidateUserId = candidateUserId,
            FailureKind = failureKind
        };
    }
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
