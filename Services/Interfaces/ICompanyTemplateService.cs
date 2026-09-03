using GloryLikeBackend.Dtos.CompanyTemplates;

namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyTemplateService
{
    Task<CompanyTemplateResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyTemplateResponse> CreateAsync(
        SaveCompanyTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyTemplateResponse> UpdateAsync(
        Guid templateId,
        SaveCompanyTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyTemplateResponse> DeleteAsync(
        int actorUserId,
        Guid templateId,
        CancellationToken cancellationToken = default);
}
