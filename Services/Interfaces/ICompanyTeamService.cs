using GloryLikeBackend.Dtos.CompanyTeam;

namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyTeamService
{
    Task<CompanyTeamResponse> GetTeamAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default);

    Task<CompanyTeamResponse> InviteAsync(
        InviteCompanyTeamMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyTeamResponse> RemoveMemberAsync(
        Guid invitationId,
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<ResolveCompanyTeamInvitationResponse> ResolveInvitationAsync(
        string token,
        CancellationToken cancellationToken = default);
}
