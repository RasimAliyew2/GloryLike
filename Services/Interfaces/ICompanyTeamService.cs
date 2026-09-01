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

    Task<CompanyTeamResponse> UpdateMemberRoleAsync(
        Guid invitationId,
        UpdateCompanyTeamMemberRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyTeamResponse> CreateRoleAsync(
        SaveCompanyAccessRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<CompanyTeamResponse> UpdateRoleAsync(
        Guid roleId,
        SaveCompanyAccessRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<ResolveCompanyTeamInvitationResponse> ResolveInvitationAsync(
        string token,
        CancellationToken cancellationToken = default);
}
