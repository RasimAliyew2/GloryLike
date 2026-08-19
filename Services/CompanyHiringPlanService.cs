using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyHiringPlan;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyHiringPlanService : ICompanyHiringPlanService
{
    private static readonly HashSet<string> FinishedVacancyStatuses =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Closed",
            "Finished",
            "Filled"
        };

    private static readonly HashSet<string> Priorities =
        new(StringComparer.Ordinal)
        {
            "Critical",
            "High",
            "Medium",
            "Low"
        };

    private static readonly HashSet<string> EmploymentTypes =
        new(StringComparer.Ordinal)
        {
            "Full-time",
            "Part-time",
            "Contract",
            "Temporary",
            "Internship"
        };

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;

    public CompanyHiringPlanService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
    }

    public async Task<CompanyHiringPlanResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(
            actorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var plans = await BaseQuery()
            .Where(item => item.CompanyOwnerUserId == ownerUserId.Value)
            .OrderBy(item => item.TargetStartDate)
            .ThenByDescending(item => item.Priority)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        return new CompanyHiringPlanResponse
        {
            Success = true,
            Message = plans.Count == 0
                ? "Hiring plan is empty."
                : $"{plans.Count} hiring plan rows loaded.",
            CompanyOwnerUserId = ownerUserId.Value,
            Plans = plans.Select(ToDto).ToList()
        };
    }

    public async Task<CompanyHiringPlanResponse> GetByIdAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(
            actorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var plan = await BaseQuery().FirstOrDefaultAsync(
            item => item.Id == planId
                && item.CompanyOwnerUserId == ownerUserId.Value,
            cancellationToken);

        return plan is null
            ? NotFound()
            : Successful(ownerUserId.Value, ToDto(plan), "Hiring plan row loaded.");
    }

    public Task<CompanyHiringPlanResponse> CreateAsync(
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(null, request, cancellationToken);
    }

    public Task<CompanyHiringPlanResponse> UpdateAsync(
        int planId,
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        return SaveAsync(planId, request, cancellationToken);
    }

    public async Task<CompanyHiringPlanResponse> DeleteAsync(
        int actorUserId,
        int planId,
        CancellationToken cancellationToken = default)
    {
        var ownerUserId = await ResolveOwnerUserIdAsync(
            actorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var plan = await _dbContext.CompanyHiringPlans
            .Include(item => item.Vacancies)
            .FirstOrDefaultAsync(
                item => item.Id == planId
                    && item.CompanyOwnerUserId == ownerUserId.Value,
                cancellationToken);

        if (plan is null)
            return NotFound();

        foreach (var vacancy in plan.Vacancies)
        {
            vacancy.HiringPlanId = null;
            vacancy.HiringPlan = null;
        }

        _dbContext.CompanyHiringPlans.Remove(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CompanyHiringPlanResponse
        {
            Success = true,
            Message = "Hiring plan row deleted.",
            CompanyOwnerUserId = ownerUserId.Value
        };
    }

    private async Task<CompanyHiringPlanResponse> SaveAsync(
        int? planId,
        SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken)
    {
        Normalize(request);
        var validationMessage = Validate(request);

        if (!string.IsNullOrWhiteSpace(validationMessage))
            return Failed(validationMessage, CompanyHiringPlanErrorCodes.Validation);

        var ownerUserId = await ResolveOwnerUserIdAsync(
            request.ActorUserId,
            cancellationToken);

        if (!ownerUserId.HasValue)
            return Forbidden();

        var taxonomy = await (
            from jobFamily in _dbContext.JobFamilies.AsNoTracking()
            join position in _dbContext.Positions.AsNoTracking()
                on jobFamily.Id equals position.JobFamilyId
            join link in _dbContext.PositionSeniorities.AsNoTracking()
                on position.Id equals link.PositionId
            join seniority in _dbContext.Seniorities.AsNoTracking()
                on link.SeniorityId equals seniority.Id
            where jobFamily.Id == request.JobFamilyId
                && position.Id == request.PositionId
                && seniority.Id == request.SeniorityId
            select new
            {
                JobFamily = jobFamily,
                Position = position,
                Seniority = seniority
            }).FirstOrDefaultAsync(cancellationToken);

        if (taxonomy is null)
        {
            return Failed(
                "The selected job, position, and seniority combination does not exist in SQL taxonomy.",
                CompanyHiringPlanErrorCodes.Validation);
        }

        CompanyHiringPlan plan;
        var now = DateTime.UtcNow;

        if (planId.HasValue)
        {
            var existingPlan = await _dbContext.CompanyHiringPlans
                .Include(item => item.Vacancies)
                .FirstOrDefaultAsync(
                    item => item.Id == planId.Value
                        && item.CompanyOwnerUserId == ownerUserId.Value,
                    cancellationToken);

            if (existingPlan is null)
                return NotFound();

            plan = existingPlan;

            if (request.Headcount < plan.Vacancies.Count)
            {
                return Failed(
                    $"Headcount cannot be lower than the {plan.Vacancies.Count} linked vacancies.",
                    CompanyHiringPlanErrorCodes.Conflict);
            }

            if (plan.Vacancies.Count > 0
                && (request.JobFamilyId != plan.JobFamilyId
                    || request.PositionId != plan.PositionId
                    || request.SeniorityId != plan.SeniorityId))
            {
                return Failed(
                    "Job, position, and seniority cannot be changed after a vacancy is linked.",
                    CompanyHiringPlanErrorCodes.Conflict);
            }
        }
        else
        {
            plan = new CompanyHiringPlan
            {
                CompanyOwnerUserId = ownerUserId.Value,
                CreatedByUserId = request.ActorUserId,
                CreatedAtUtc = now
            };
            _dbContext.CompanyHiringPlans.Add(plan);
        }

        plan.JobFamilyId = request.JobFamilyId;
        plan.PositionId = request.PositionId;
        plan.SeniorityId = request.SeniorityId;
        plan.Headcount = request.Headcount;
        plan.Priority = request.Priority;
        plan.TargetStartDate = request.TargetStartDate?.Date;
        plan.EmploymentType = request.EmploymentType;
        plan.Notes = request.Notes ?? string.Empty;
        plan.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        plan.JobFamily = taxonomy.JobFamily;
        plan.Position = taxonomy.Position;
        plan.Seniority = taxonomy.Seniority;
        plan.Vacancies ??= new();

        return Successful(
            ownerUserId.Value,
            ToDto(plan),
            planId.HasValue
                ? "Hiring plan row updated."
                : "Hiring plan row created.");
    }

    private IQueryable<CompanyHiringPlan> BaseQuery()
    {
        return _dbContext.CompanyHiringPlans
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.JobFamily)
            .Include(item => item.Position)
            .Include(item => item.Seniority)
            .Include(item => item.Vacancies);
    }

    private async Task<int?> ResolveOwnerUserIdAsync(
        int actorUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0)
            return null;

        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);
        return access?.CompanyOwnerUserId;
    }

    private static CompanyHiringPlanDto ToDto(CompanyHiringPlan plan)
    {
        var vacancyCount = plan.Vacancies.Count;
        var finishedCount = plan.Vacancies.Count(vacancy =>
            FinishedVacancyStatuses.Contains(vacancy.Status));
        var status = vacancyCount == 0
            ? CompanyHiringPlanStatuses.Planned
            : vacancyCount >= plan.Headcount && finishedCount == vacancyCount
                ? CompanyHiringPlanStatuses.Finished
                : CompanyHiringPlanStatuses.InProgress;

        return new CompanyHiringPlanDto
        {
            Id = plan.Id,
            JobFamilyId = plan.JobFamilyId,
            JobFamilyName = plan.JobFamily.JobName,
            PositionId = plan.PositionId,
            PositionName = plan.Position.Name,
            SeniorityId = plan.SeniorityId,
            SeniorityName = plan.Seniority.Name,
            Headcount = plan.Headcount,
            Priority = plan.Priority,
            TargetStartDate = plan.TargetStartDate,
            EmploymentType = plan.EmploymentType,
            Notes = plan.Notes,
            Status = status,
            VacancyCount = vacancyCount,
            FinishedVacancyCount = finishedCount,
            RemainingVacancyCount = Math.Max(0, plan.Headcount - vacancyCount),
            CanCreateVacancy = vacancyCount < plan.Headcount,
            CreatedAtUtc = plan.CreatedAtUtc,
            UpdatedAtUtc = plan.UpdatedAtUtc,
            Vacancies = plan.Vacancies
                .OrderBy(vacancy => vacancy.CreatedAtUtc)
                .Select(vacancy => new CompanyHiringPlanVacancyDto
                {
                    VacancyId = vacancy.Id,
                    PlatformVacancyId = vacancy.PlatformVacancyId,
                    RoleTitle = vacancy.RoleTitle,
                    Status = vacancy.Status
                })
                .ToList()
        };
    }

    private static void Normalize(SaveCompanyHiringPlanRequest request)
    {
        request.Priority = request.Priority?.Trim() ?? string.Empty;
        request.EmploymentType = request.EmploymentType?.Trim() ?? string.Empty;
        request.Notes = request.Notes?.Trim();
    }

    private static string Validate(SaveCompanyHiringPlanRequest request)
    {
        if (request.ActorUserId <= 0
            || request.JobFamilyId <= 0
            || request.PositionId <= 0
            || request.SeniorityId <= 0)
        {
            return "Actor, job, position, and seniority are required.";
        }

        if (request.Headcount is < 1 or > 1000)
            return "Headcount must be between 1 and 1000.";

        if (!Priorities.Contains(request.Priority))
            return "Priority must be Critical, High, Medium, or Low.";

        if (!EmploymentTypes.Contains(request.EmploymentType))
            return "Employment type is not supported.";

        if ((request.Notes?.Length ?? 0) > 1000)
            return "Notes can contain at most 1000 characters.";

        return string.Empty;
    }

    private static CompanyHiringPlanResponse Successful(
        int ownerUserId,
        CompanyHiringPlanDto plan,
        string message)
    {
        return new CompanyHiringPlanResponse
        {
            Success = true,
            Message = message,
            CompanyOwnerUserId = ownerUserId,
            Plan = plan
        };
    }

    private static CompanyHiringPlanResponse Forbidden() => Failed(
        "You do not have access to this company's hiring plan.",
        CompanyHiringPlanErrorCodes.Forbidden);

    private static CompanyHiringPlanResponse NotFound() => Failed(
        "Hiring plan row was not found.",
        CompanyHiringPlanErrorCodes.NotFound);

    private static CompanyHiringPlanResponse Failed(
        string message,
        string errorCode)
    {
        return new CompanyHiringPlanResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
