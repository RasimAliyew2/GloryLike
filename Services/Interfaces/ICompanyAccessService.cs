namespace GloryLikeBackend.Services.Interfaces;

public interface ICompanyAccessService
{
    Task<CompanyAccessContext?> ResolveAsync(
        int actorUserId,
        CancellationToken cancellationToken = default);

    Task<List<int>> GetActiveUserIdsAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken = default);
}

public sealed record CompanyAccessContext(
    int ActorUserId,
    int CompanyOwnerUserId,
    string Role,
    bool IsFounder,
    bool CanManageTeam);
