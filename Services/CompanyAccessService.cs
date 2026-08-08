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
            .Where(item => item.AcceptedUserId == actorUserId)
            .OrderByDescending(item => item.AcceptedAtUtc)
            .Select(item => new
            {
                item.OwnerUserId,
                item.Role,
                item.Status
            })
            .ToListAsync(cancellationToken);

        var activeMembership = memberships.FirstOrDefault(
            item => item.Status == CompanyTeamInvitationStatuses.Active);

        if (activeMembership is not null)
        {
            var isHrAdmin = string.Equals(
                activeMembership.Role,
                "HR Admin",
                StringComparison.OrdinalIgnoreCase);

            return new CompanyAccessContext(
                actorUserId,
                activeMembership.OwnerUserId,
                activeMembership.Role,
                IsFounder: false,
                CanManageTeam: isHrAdmin);
        }

        // A removed team member must not silently become a new Founder.
        if (memberships.Count > 0)
            return null;

        return new CompanyAccessContext(
            actorUserId,
            actorUserId,
            "Admin",
            IsFounder: true,
            CanManageTeam: true);
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
