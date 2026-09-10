using GloryLikeBackend.Dtos.CompanyTemplates;

namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyFunnelService
{
    Task<CompanyFunnelResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyFunnelResponse> CreateAsync(
        SaveCompanyFunnelRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyFunnelResponse> UpdateAsync(
        Guid templateId,
        SaveCompanyFunnelRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyFunnelResponse> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default);
}
