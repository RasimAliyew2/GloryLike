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

    Task<EmployerVacancyDetailResponse?> GetEmployerVacancyDetailAsync(
        int employerUserId,
        int vacancyId,
        CancellationToken cancellationToken = default);

    Task<EmployerVacancyEditResponse?> GetEmployerVacancyForEditAsync(
        int employerUserId,
        int vacancyId,
        CancellationToken cancellationToken = default);

    Task<ToggleEmployerVacancyStatusResult> ToggleEmployerStatusAsync(
        int employerUserId,
        int vacancyId,
        CancellationToken cancellationToken = default);

    Task<ApplyToVacancyResult> ApplyToVacancyAsync(
        int vacancyId,
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<CreateVacancyResult> CreateAsync(
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default);

    Task<UpdateVacancyResult> UpdateAsync(
        int vacancyId,
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default);
}

public enum ToggleEmployerVacancyStatusFailureKind
{
    None = 0,
    Validation = 1,
    NotFound = 2
}

public sealed class ToggleEmployerVacancyStatusResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int VacancyId { get; private set; }
    public int EmployerUserId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; private set; }
    public ToggleEmployerVacancyStatusFailureKind FailureKind { get; private set; }

    public bool IsSuspended => Status.Equals(
        "Suspended",
        StringComparison.OrdinalIgnoreCase);

    public static ToggleEmployerVacancyStatusResult Updated(
        Vacancy vacancy)
    {
        var isSuspended = vacancy.Status.Equals(
            "Suspended",
            StringComparison.OrdinalIgnoreCase);

        return new ToggleEmployerVacancyStatusResult
        {
            Success = true,
            Message = isSuspended
                ? "Vacancy dayandırıldı."
                : "Vacancy yenidən aktiv edildi.",
            VacancyId = vacancy.Id,
            EmployerUserId = vacancy.EmployerUserId,
            Status = vacancy.Status,
            UpdatedAtUtc = vacancy.UpdatedAtUtc
        };
    }

    public static ToggleEmployerVacancyStatusResult Invalid(
        int employerUserId,
        int vacancyId,
        string message)
    {
        return Failed(
            employerUserId,
            vacancyId,
            message,
            ToggleEmployerVacancyStatusFailureKind.Validation);
    }

    public static ToggleEmployerVacancyStatusResult NotFound(
        int employerUserId,
        int vacancyId,
        string message)
    {
        return Failed(
            employerUserId,
            vacancyId,
            message,
            ToggleEmployerVacancyStatusFailureKind.NotFound);
    }

    private static ToggleEmployerVacancyStatusResult Failed(
        int employerUserId,
        int vacancyId,
        string message,
        ToggleEmployerVacancyStatusFailureKind failureKind)
    {
        return new ToggleEmployerVacancyStatusResult
        {
            Success = false,
            Message = message,
            VacancyId = vacancyId,
            EmployerUserId = employerUserId,
            FailureKind = failureKind
        };
    }
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

public enum UpdateVacancyFailureKind
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3
}

public sealed class UpdateVacancyResult
{
    public bool Success { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public int VacancyId { get; private set; }
    public int EmployerUserId { get; private set; }
    public string PlatformVacancyId { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; private set; }
    public UpdateVacancyFailureKind FailureKind { get; private set; }

    public static UpdateVacancyResult Updated(Vacancy vacancy)
    {
        return new UpdateVacancyResult
        {
            Success = true,
            Message = "Vacancy dəyişiklikləri SQL-də saxlanıldı.",
            VacancyId = vacancy.Id,
            EmployerUserId = vacancy.EmployerUserId,
            PlatformVacancyId = vacancy.PlatformVacancyId,
            Status = vacancy.Status,
            UpdatedAtUtc = vacancy.UpdatedAtUtc
        };
    }

    public static UpdateVacancyResult Invalid(
        int employerUserId,
        int vacancyId,
        string message)
    {
        return Failed(
            employerUserId,
            vacancyId,
            message,
            UpdateVacancyFailureKind.Validation);
    }

    public static UpdateVacancyResult NotFound(
        int employerUserId,
        int vacancyId,
        string message)
    {
        return Failed(
            employerUserId,
            vacancyId,
            message,
            UpdateVacancyFailureKind.NotFound);
    }

    public static UpdateVacancyResult Conflict(
        int employerUserId,
        int vacancyId,
        string message)
    {
        return Failed(
            employerUserId,
            vacancyId,
            message,
            UpdateVacancyFailureKind.Conflict);
    }

    private static UpdateVacancyResult Failed(
        int employerUserId,
        int vacancyId,
        string message,
        UpdateVacancyFailureKind failureKind)
    {
        return new UpdateVacancyResult
        {
            Success = false,
            Message = message,
            VacancyId = vacancyId,
            EmployerUserId = employerUserId,
            FailureKind = failureKind
        };
    }
}
