using GloryLikeBackend.Dtos.CompanyProfile;

namespace GloryLikeBackend.Services.Interfaces;

public interface IOpenAiCompanyAboutPageDesigner
{
    Task<CustomizeCompanyAboutPageResponse> CustomizeAsync(
        CustomizeCompanyAboutPageRequest request,
        CancellationToken cancellationToken = default);
}
