using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Reports;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class OrganizationReportsService : IOrganizationReportsService
{
    private const string VacancyCreationReportKey =
        "vacancy-creation-by-employee";
    private const string VacancyCreationReportTitle =
        "Vacancies created by employees";

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;

    public OrganizationReportsService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
    }

    public async Task<OrganizationReportCatalogResponse> GetCatalogAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);

        if (access is null)
        {
            return CatalogFailure(
                "You do not have access to this company's reports.",
                ReportFailureCodes.Forbidden);
        }

        var owner = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == access.CompanyOwnerUserId,
                cancellationToken);

        if (owner is null)
        {
            return CatalogFailure(
                "Company owner was not found.",
                ReportFailureCodes.NotFound);
        }

        return new OrganizationReportCatalogResponse
        {
            Success = true,
            Message = "Report catalog loaded.",
            CompanyName = BuildCompanyName(owner),
            Reports =
            [
                new OrganizationReportDefinitionDto
                {
                    Key = VacancyCreationReportKey,
                    Title = VacancyCreationReportTitle,
                    Description =
                        "Vacancy creation activity grouped by company employee."
                }
            ]
        };
    }

    public async Task<VacancyCreationReportResponse>
        ExecuteVacancyCreationReportAsync(
            int actorUserId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
        {
            return VacancyReportFailure(
                "Employer user ID is invalid.",
                ReportFailureCodes.Validation);
        }

        if (dateFrom == default || dateTo == default)
        {
            return VacancyReportFailure(
                "Both period dates are required.",
                ReportFailureCodes.Validation);
        }

        var from = DateTime.SpecifyKind(dateFrom.Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(dateTo.Date, DateTimeKind.Utc);

        if (from > to)
        {
            return VacancyReportFailure(
                "Period start date cannot be later than end date.",
                ReportFailureCodes.Validation);
        }

        if (to == DateTime.MaxValue.Date)
        {
            return VacancyReportFailure(
                "Period end date is outside the supported range.",
                ReportFailureCodes.Validation);
        }

        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);

        if (access is null)
        {
            return VacancyReportFailure(
                "You do not have access to this company's reports.",
                ReportFailureCodes.Forbidden);
        }

        var owner = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == access.CompanyOwnerUserId,
                cancellationToken);

        if (owner is null)
        {
            return VacancyReportFailure(
                "Company owner was not found.",
                ReportFailureCodes.NotFound);
        }

        var toExclusive = to.AddDays(1);
        var vacancyRows = await _dbContext.Vacancies
            .AsNoTracking()
            .Where(vacancy =>
                vacancy.CompanyOwnerUserId == access.CompanyOwnerUserId
                && vacancy.CreatedAtUtc >= from
                && vacancy.CreatedAtUtc < toExclusive)
            .Select(vacancy => new VacancyProjection
            {
                Id = vacancy.Id,
                EmployerUserId = vacancy.EmployerUserId,
                PlatformVacancyId = vacancy.PlatformVacancyId,
                RoleTitle = vacancy.RoleTitle,
                PositionName = vacancy.PositionName,
                Status = vacancy.Status,
                CreatedAtUtc = vacancy.CreatedAtUtc
            })
            .OrderBy(vacancy => vacancy.CreatedAtUtc)
            .ThenBy(vacancy => vacancy.Id)
            .ToListAsync(cancellationToken);

        var memberships = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Where(invitation =>
                invitation.OwnerUserId == access.CompanyOwnerUserId
                && invitation.AcceptedUserId.HasValue)
            .Select(invitation => new MembershipProjection
            {
                UserId = invitation.AcceptedUserId!.Value,
                Role = invitation.Role,
                Status = invitation.Status,
                UpdatedAtUtc = invitation.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var employeeIds = memberships
            .Where(membership =>
                membership.Status == CompanyTeamInvitationStatuses.Active)
            .Select(membership => membership.UserId)
            .Concat(vacancyRows.Select(vacancy => vacancy.EmployerUserId))
            .Append(access.CompanyOwnerUserId)
            .Distinct()
            .ToList();

        var employees = await _dbContext.Users
            .AsNoTracking()
            .Where(user => employeeIds.Contains(user.Id))
            .Select(user => new UserProjection
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Surname = user.Surname,
                Email = user.Email
            })
            .ToListAsync(cancellationToken);

        var latestMembershipByUser = memberships
            .GroupBy(membership => membership.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(membership => membership.UpdatedAtUtc)
                    .First());
        var vacanciesByCreator = vacancyRows
            .GroupBy(vacancy => vacancy.EmployerUserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(ToVacancyItem).ToList());

        var reportEmployees = employees
            .Select(employee =>
            {
                var employeeVacancies = vacanciesByCreator.GetValueOrDefault(
                    employee.Id)
                    ?? [];
                var membership = latestMembershipByUser.GetValueOrDefault(
                    employee.Id);

                return new VacancyCreatorReportRowDto
                {
                    UserId = employee.Id,
                    DisplayName = BuildDisplayName(employee),
                    Email = employee.Email,
                    Role = employee.Id == access.CompanyOwnerUserId
                        ? "Admin"
                        : NormalizeRole(membership?.Role),
                    MembershipStatus = employee.Id == access.CompanyOwnerUserId
                        ? CompanyTeamInvitationStatuses.Active
                        : NormalizeMembershipStatus(membership?.Status),
                    VacancyCount = employeeVacancies.Count,
                    VacancyCreationDatesUtc = employeeVacancies
                        .Select(vacancy => vacancy.CreatedAtUtc)
                        .ToList(),
                    Vacancies = employeeVacancies
                };
            })
            .OrderByDescending(employee => employee.VacancyCount)
            .ThenBy(employee => employee.DisplayName)
            .ToList();

        return new VacancyCreationReportResponse
        {
            Success = true,
            Message = "Report generated from SQL data.",
            CompanyName = BuildCompanyName(owner),
            ReportTitle = VacancyCreationReportTitle,
            DateFrom = from,
            DateTo = to,
            GeneratedAtUtc = DateTime.UtcNow,
            TotalVacancyCount = vacancyRows.Count,
            Employees = reportEmployees
        };
    }

    public async Task<ReportEmployeeProfileResponse> GetEmployeeProfileAsync(
        int actorUserId,
        int employeeUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0 || employeeUserId <= 0)
        {
            return EmployeeProfileFailure(
                "Employer and employee user IDs must be valid.",
                ReportFailureCodes.Validation);
        }

        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);

        if (access is null)
        {
            return EmployeeProfileFailure(
                "You do not have access to this employee profile.",
                ReportFailureCodes.Forbidden);
        }

        var isOwner = employeeUserId == access.CompanyOwnerUserId;
        var membership = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Where(invitation =>
                invitation.OwnerUserId == access.CompanyOwnerUserId
                && invitation.AcceptedUserId == employeeUserId)
            .OrderByDescending(invitation => invitation.UpdatedAtUtc)
            .Select(invitation => new MembershipProjection
            {
                UserId = employeeUserId,
                Role = invitation.Role,
                Status = invitation.Status,
                UpdatedAtUtc = invitation.UpdatedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        var hasCreatedCompanyVacancy = !isOwner
            && membership is null
            && await _dbContext.Vacancies
                .AsNoTracking()
                .AnyAsync(
                    vacancy =>
                        vacancy.CompanyOwnerUserId == access.CompanyOwnerUserId
                        && vacancy.EmployerUserId == employeeUserId,
                    cancellationToken);

        if (!isOwner && membership is null && !hasCreatedCompanyVacancy)
        {
            return EmployeeProfileFailure(
                "This user does not belong to your company report.",
                ReportFailureCodes.Forbidden);
        }

        var ownerAndEmployee = await _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == access.CompanyOwnerUserId
                || user.Id == employeeUserId)
            .ToListAsync(cancellationToken);
        var owner = ownerAndEmployee.FirstOrDefault(
            user => user.Id == access.CompanyOwnerUserId);
        var employee = ownerAndEmployee.FirstOrDefault(
            user => user.Id == employeeUserId);

        if (owner is null || employee is null)
        {
            return EmployeeProfileFailure(
                "Employee profile was not found.",
                ReportFailureCodes.NotFound);
        }

        var createdVacancyCount = await _dbContext.Vacancies
            .AsNoTracking()
            .CountAsync(
                vacancy =>
                    vacancy.CompanyOwnerUserId == access.CompanyOwnerUserId
                    && vacancy.EmployerUserId == employeeUserId,
                cancellationToken);

        return new ReportEmployeeProfileResponse
        {
            Success = true,
            Message = "Employee profile loaded.",
            CompanyName = BuildCompanyName(owner),
            UserId = employee.Id,
            DisplayName = BuildDisplayName(employee),
            UserName = employee.UserName,
            Email = employee.Email,
            Role = isOwner ? "Admin" : NormalizeRole(membership?.Role),
            MembershipStatus = isOwner
                ? CompanyTeamInvitationStatuses.Active
                : membership is null
                    ? "Former member"
                    : NormalizeMembershipStatus(membership.Status),
            BirthDate = employee.BirthDate,
            About = employee.About ?? string.Empty,
            ProfileImageDataUrl = employee.ProfileImageDataUrl ?? string.Empty,
            CreatedVacancyCount = createdVacancyCount
        };
    }

    private static VacancyCreationReportItemDto ToVacancyItem(
        VacancyProjection vacancy)
    {
        return new VacancyCreationReportItemDto
        {
            VacancyId = vacancy.Id,
            PlatformVacancyId = vacancy.PlatformVacancyId,
            Title = string.IsNullOrWhiteSpace(vacancy.RoleTitle)
                ? vacancy.PositionName
                : vacancy.RoleTitle,
            Status = vacancy.Status,
            CreatedAtUtc = vacancy.CreatedAtUtc
        };
    }

    private static string BuildCompanyName(User owner)
    {
        if (!string.IsNullOrWhiteSpace(owner.CompanyName))
            return owner.CompanyName.Trim();

        return BuildDisplayName(owner);
    }

    private static string BuildDisplayName(User user)
    {
        var fullName = string.Join(
            " ",
            new[] { user.Name, user.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()));

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return string.IsNullOrWhiteSpace(user.UserName)
            ? user.Email
            : user.UserName.Trim();
    }

    private static string BuildDisplayName(UserProjection user)
    {
        var fullName = string.Join(
            " ",
            new[] { user.Name, user.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim()));

        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        return string.IsNullOrWhiteSpace(user.UserName)
            ? user.Email
            : user.UserName.Trim();
    }

    private static string NormalizeRole(string? role)
    {
        return string.IsNullOrWhiteSpace(role) ? "Former member" : role.Trim();
    }

    private static string NormalizeMembershipStatus(string? status)
    {
        return string.IsNullOrWhiteSpace(status) ? "Former member" : status.Trim();
    }

    private static OrganizationReportCatalogResponse CatalogFailure(
        string message,
        string errorCode)
    {
        return new OrganizationReportCatalogResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    private static VacancyCreationReportResponse VacancyReportFailure(
        string message,
        string errorCode)
    {
        return new VacancyCreationReportResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    private static ReportEmployeeProfileResponse EmployeeProfileFailure(
        string message,
        string errorCode)
    {
        return new ReportEmployeeProfileResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    private sealed class VacancyProjection
    {
        public int Id { get; set; }
        public int EmployerUserId { get; set; }
        public string PlatformVacancyId { get; set; } = string.Empty;
        public string RoleTitle { get; set; } = string.Empty;
        public string PositionName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
    }

    private sealed class MembershipProjection
    {
        public int UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime UpdatedAtUtc { get; set; }
    }

    private sealed class UserProjection
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    private static class ReportFailureCodes
    {
        public const string Validation = "validation";
        public const string Forbidden = "forbidden";
        public const string NotFound = "not_found";
    }
}
