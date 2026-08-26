using GloryLikeBackend.Dtos.Reports;

namespace GloryLikeBackend.Services.Interfaces;

public interface IOrganizationReportsService
{
    Task<OrganizationReportCatalogResponse> GetCatalogAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<VacancyCreationReportResponse> ExecuteVacancyCreationReportAsync(
        int actorUserId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<ReportEmployeeProfileResponse> GetEmployeeProfileAsync(
        int actorUserId,
        int employeeUserId,
        CancellationToken cancellationToken = default);
}
