using GloryLikeBackend.Dtos.CompanyProfile;

namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyProfileService
{
    Task<CompanyProfileResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyProfileResponse> SaveAsync(
        SaveCompanyProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<PublicCompanyProfileResponse> GetPublicAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken = default);
}
