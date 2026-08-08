using System.Globalization;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Reports;
using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Vacancies;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class OrganizationReportsService : IOrganizationReportsService
{
    private static readonly string[] ActiveVacancyStatuses =
    [
        "Published",
        "Active"
    ];

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;

    public OrganizationReportsService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
    }

    public async Task<OrganizationReportsResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);

        if (access is null)
        {
            return new OrganizationReportsResponse
            {
                Success = false,
                ErrorCode = "forbidden",
                Message = "Bu təşkilatın report-larına giriş icazəniz yoxdur."
            };
        }

        var owner = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == access.CompanyOwnerUserId,
                cancellationToken);

        if (owner is null)
        {
            return new OrganizationReportsResponse
            {
                Success = false,
                ErrorCode = "not_found",
                Message = "Company owner tapılmadı."
            };
        }

        var vacancies = await _dbContext.Vacancies
            .AsNoTracking()
            .Where(item =>
                item.CompanyOwnerUserId == access.CompanyOwnerUserId)
            .Select(item => new
            {
                item.Id,
                item.Status,
                item.CreatedAtUtc,
                item.ApplicationDeadline,
                ApplicationCount = item.Applications.Count
            })
            .ToListAsync(cancellationToken);

        var vacancyIds = vacancies.Select(item => item.Id).ToList();
        var applications = vacancyIds.Count == 0
            ? new List<VacancyApplication>()
            : await _dbContext.VacancyApplications
                .AsNoTracking()
                .Where(item => vacancyIds.Contains(item.VacancyId))
                .ToListAsync(cancellationToken);

        var team = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Where(item =>
                item.OwnerUserId == access.CompanyOwnerUserId
                && item.Status != CompanyTeamInvitationStatuses.Removed)
            .Select(item => new
            {
                item.Role,
                item.Status
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = now.Date;
        var activeVacancies = vacancies.Count(item =>
            ActiveVacancyStatuses.Contains(
                item.Status,
                StringComparer.OrdinalIgnoreCase));
        var suspendedVacancies = vacancies.Count(item =>
            item.Status.Equals(
                "Suspended",
                StringComparison.OrdinalIgnoreCase)
            || item.Status.Equals(
                "Paused",
                StringComparison.OrdinalIgnoreCase));
        var averageApplications = vacancies.Count == 0
            ? 0
            : applications.Count / (double)vacancies.Count;
        var activeTeam = team.Count(item =>
            item.Status == CompanyTeamInvitationStatuses.Active);
        var invitedTeam = team.Count(item =>
            item.Status == CompanyTeamInvitationStatuses.Invited);

        return new OrganizationReportsResponse
        {
            Success = true,
            Message = "Təşkilat report-ları SQL-dən yaradıldı.",
            CompanyName = string.IsNullOrWhiteSpace(owner.CompanyName)
                ? owner.Name.Trim()
                : owner.CompanyName.Trim(),
            GeneratedAtUtc = now,
            Categories =
            [
                Category(
                    "recruitment",
                    "Recruitment overview",
                    "Vakansiyaların ümumi vəziyyəti və son tarixlər.",
                    Metric("total-vacancies", "Total vacancies", vacancies.Count, "Bütün team tərəfindən yaradılan", "primary"),
                    Metric("active-vacancies", "Active vacancies", activeVacancies, "Published və Active", "success"),
                    Metric("suspended-vacancies", "Suspended vacancies", suspendedVacancies, "Paused və Suspended", "warning"),
                    Metric(
                        "closing-soon",
                        "Closing in 7 days",
                        vacancies.Count(item =>
                            item.ApplicationDeadline.HasValue
                            && item.ApplicationDeadline.Value.Date >= today
                            && item.ApplicationDeadline.Value.Date <= today.AddDays(7)),
                        "Application deadline yaxınlaşır",
                        "warning")),

                Category(
                    "pipeline",
                    "Candidate pipeline",
                    "Namizəd müraciətlərinin cari vəziyyəti.",
                    Metric("total-applications", "Total applications", applications.Count, "Bütün təşkilat vakansiyaları", "primary"),
                    Metric(
                        "new-applications",
                        "Applications in 30 days",
                        applications.Count(item => item.AppliedAtUtc >= now.AddDays(-30)),
                        "Son 30 gün",
                        "success"),
                    Metric(
                        "awaiting-response",
                        "Awaiting response",
                        applications.Count(item => item.Status == "NoResponseYet"),
                        "İlkin cavab gözləyir",
                        "warning"),
                    Metric(
                        "applications-per-vacancy",
                        "Applications per vacancy",
                        averageApplications.ToString("0.0", CultureInfo.InvariantCulture),
                        "Orta göstərici",
                        "neutral")),

                Category(
                    "performance",
                    "Hiring performance",
                    "Diqqət tələb edən vakansiyalar və son aktivlik.",
                    Metric(
                        "new-vacancies",
                        "Vacancies created in 30 days",
                        vacancies.Count(item => item.CreatedAtUtc >= now.AddDays(-30)),
                        "Son 30 gün",
                        "success"),
                    Metric(
                        "zero-candidates",
                        "Vacancies with no candidates",
                        vacancies.Count(item => item.ApplicationCount == 0),
                        "Sourcing tələb edir",
                        "warning"),
                    Metric(
                        "past-deadline",
                        "Past deadline",
                        vacancies.Count(item =>
                            item.ApplicationDeadline.HasValue
                            && item.ApplicationDeadline.Value.Date < today
                            && ActiveVacancyStatuses.Contains(
                                item.Status,
                                StringComparer.OrdinalIgnoreCase)),
                        "Statusu yenilənməlidir",
                        "warning")),

                Category(
                    "team",
                    "Team & access",
                    "Təşkilat üzvləri və rol bölgüsü.",
                    Metric("active-members", "Active members", activeTeam + 1, "Admin daxil olmaqla", "primary"),
                    Metric("pending-invitations", "Pending invitations", invitedTeam, "Qəbul gözləyir", "warning"),
                    Metric(
                        "hr-admins",
                        "HR Admins",
                        team.Count(item =>
                            item.Status == CompanyTeamInvitationStatuses.Active
                            && item.Role == "HR Admin"),
                        "Team idarəetmə icazəsi",
                        "neutral"),
                    Metric(
                        "hiring-team",
                        "Hiring managers & recruiters",
                        team.Count(item =>
                            item.Status == CompanyTeamInvitationStatuses.Active
                            && (item.Role == "Hiring Manager"
                                || item.Role == "Recruiter")),
                        "Aktiv hiring team",
                        "neutral"))
            ]
        };
    }

    private static OrganizationReportCategoryDto Category(
        string key,
        string title,
        string description,
        params OrganizationReportMetricDto[] metrics)
    {
        return new OrganizationReportCategoryDto
        {
            Key = key,
            Title = title,
            Description = description,
            Metrics = metrics.ToList()
        };
    }

    private static OrganizationReportMetricDto Metric(
        string key,
        string label,
        object value,
        string detail,
        string tone)
    {
        return new OrganizationReportMetricDto
        {
            Key = key,
            Label = label,
            Value = Convert.ToString(value, CultureInfo.InvariantCulture)
                ?? "0",
            Detail = detail,
            Tone = tone
        };
    }
}
