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
    Guid? RoleId,
    string Scope,
    bool IsFounder,
    bool IsFullAccess,
    IReadOnlySet<string> Permissions)
{
    public bool CanManageTeam =>
        IsFounder
        || IsFullAccess
        || Permissions.Contains("team.participants.view")
        || Permissions.Contains("team.participants.invite")
        || Permissions.Contains("team.participants.deactivate")
        || Permissions.Contains("team.roles.assign")
        || Permissions.Contains("team.roles.manage");

    public bool CanManageRoles =>
        IsFounder
        || IsFullAccess
        || Permissions.Contains("team.roles.manage");

    public bool CanInvite =>
        IsFounder
        || IsFullAccess
        || Permissions.Contains("team.participants.invite");

    public bool CanAssignRoles =>
        IsFounder
        || IsFullAccess
        || Permissions.Contains("team.roles.assign");

    public bool CanDeactivate =>
        IsFounder
        || IsFullAccess
        || Permissions.Contains("team.participants.deactivate");

    public bool CanManageTemplates =>
        IsFounder
        || IsFullAccess
        || Permissions.Contains("company.templates_manage");
}
