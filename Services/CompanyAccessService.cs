using GloryLikeBackend.Data;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyAccessService : ICompanyAccessService
{
    private readonly AppDbContext _dbContext;

    public CompanyAccessService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CompanyAccessContext?> ResolveAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0)
            return null;

        var actor = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == actorUserId,
                cancellationToken);

        if (actor is null
            || !string.Equals(
                actor.AccountType,
                "employer",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var memberships = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Include(item => item.AccessRole)
            .ThenInclude(role => role!.Permissions)
            .Where(item => item.AcceptedUserId == actorUserId)
            .OrderByDescending(item => item.AcceptedAtUtc)
            .ToListAsync(cancellationToken);

        var activeMembership = memberships.FirstOrDefault(
            item => item.Status == CompanyTeamInvitationStatuses.Active);

        if (activeMembership is not null)
        {
            var isLegacyHrAdmin = string.Equals(
                activeMembership.AccessRole?.Name ?? activeMembership.Role,
                "HR Admin",
                StringComparison.OrdinalIgnoreCase);

            return new CompanyAccessContext(
                actorUserId,
                activeMembership.OwnerUserId,
                activeMembership.AccessRole?.Name ?? activeMembership.Role,
                activeMembership.AccessRoleId,
                activeMembership.AccessRole?.Scope
                    ?? CompanyAccessRoleScopes.Company,
                IsFounder: false,
                IsFullAccess: activeMembership.AccessRole?.IsFullAccess == true
                    || isLegacyHrAdmin,
                Permissions: new HashSet<string>(
                    activeMembership.AccessRole?.Permissions
                        .Select(permission => permission.PermissionKey)
                        ?? [],
                    StringComparer.OrdinalIgnoreCase));
        }

        // A removed team member must not silently become a new Founder.
        if (memberships.Count > 0)
            return null;

        return new CompanyAccessContext(
            actorUserId,
            actorUserId,
            "HR Admin",
            RoleId: null,
            Scope: CompanyAccessRoleScopes.Company,
            IsFounder: true,
            IsFullAccess: true,
            Permissions: CompanyAccessPermissionCatalog.AllKeys);
    }

    public async Task<List<int>> GetActiveUserIdsAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken = default)
    {
        if (companyOwnerUserId <= 0)
            return [];

        var memberIds = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Where(item =>
                item.OwnerUserId == companyOwnerUserId
                && item.Status == CompanyTeamInvitationStatuses.Active
                && item.AcceptedUserId.HasValue)
            .Select(item => item.AcceptedUserId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        memberIds.Add(companyOwnerUserId);

        return memberIds
            .Distinct()
            .ToList();
    }
}
