using GloryLikeBackend.Dtos.EmployerCandidates;

namespace GloryLikeBackend.Services.Interfaces;

public interface IEmployerCandidateMessagingService
{
    Task<EmployerCandidateProfileResponse> GetCandidateProfileAsync(
        int actorUserId,
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyMessagingOverviewResponse> GetOverviewAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyUnreadCountResponse> GetUnreadCountAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyMessageThreadResponse> GetThreadAsync(
        int actorUserId,
        int otherUserId,
        int candidateUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyMessageActionResponse> SendAsync(
        SendCompanyCandidateMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyMessageActionResponse> MarkThreadReadAsync(
        MarkCompanyMessageThreadReadRequest request,
        CancellationToken cancellationToken = default);
}
