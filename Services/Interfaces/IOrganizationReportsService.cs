using GloryLikeBackend.Dtos.Reports;

namespace GloryLikeBackend.Services.Interfaces;

public interface IOrganizationReportsService
{
    Task<OrganizationReportsResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);
}
