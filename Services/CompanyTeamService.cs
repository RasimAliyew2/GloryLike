using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyTeam;
using GloryLikeBackend.Models;
using GloryLikeBackend.Options;
using GloryLikeBackend.Services.Interfaces;
using GloryLikeBackend.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GloryLikeBackend.Services;

public sealed class CompanyTeamService : ICompanyTeamService
{
    private readonly AppDbContext _dbContext;
    private readonly IRegistrationEmailSender _emailSender;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly TeamInvitationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CompanyTeamService> _logger;

    public CompanyTeamService(
        AppDbContext dbContext,
        IRegistrationEmailSender emailSender,
        ICompanyAccessService companyAccessService,
        IOptions<TeamInvitationOptions> options,
        TimeProvider timeProvider,
        ILogger<CompanyTeamService> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _companyAccessService = companyAccessService;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CompanyTeamResponse> GetTeamAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(
            ownerUserId,
            cancellationToken);

        if (access is null)
        {
            return Failed(
                "Bu company team-ə giriş icazəniz yoxdur.",
                CompanyTeamErrorCodes.Forbidden);
        }

        var owner = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == access.CompanyOwnerUserId,
                cancellationToken);

        if (owner is null)
        {
            return Failed(
                "Company owner tapılmadı.",
                CompanyTeamErrorCodes.NotFound);
        }

        var defaultRole = await EnsureDefaultRoleAsync(
            access.CompanyOwnerUserId,
            access.ActorUserId,
            cancellationToken);

        var invitations = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Include(item => item.AcceptedUser)
            .Include(item => item.AccessRole)
            .Where(item =>
                item.OwnerUserId == access.CompanyOwnerUserId
                && item.Status
                    != CompanyTeamInvitationStatuses.Removed)
            .OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var roles = await LoadRolesAsync(
            access.CompanyOwnerUserId,
            cancellationToken);
        var history = await LoadHistoryAsync(
            access.CompanyOwnerUserId,
            cancellationToken);

        return new CompanyTeamResponse
        {
            Success = true,
            CompanyName = GetCompanyName(owner),
            CanManageTeam = access.CanManageTeam,
            CanManageRoles = access.CanManageRoles,
            CanInvite = access.CanInvite,
            ActorRole = access.IsFounder ? defaultRole.Name : access.Role,
            Members = new[] { ToFounderMemberDto(owner, defaultRole) }
                .Concat(invitations.Select(invitation => ToMemberDto(
                    invitation,
                    CanChangeRole(access, invitation),
                    CanRemoveMember(access, invitation),
                    roles.Select(role => role.Name).ToList())))
                .ToList(),
            Roles = roles,
            History = history,
            PermissionGroups = BuildPermissionGroups()
        };
    }

    public async Task<CompanyTeamResponse> InviteAsync(
        InviteCompanyTeamMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Email =
            request.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;
        if (request.OwnerUserId <= 0
            || string.IsNullOrWhiteSpace(request.Email))
        {
            return Failed(
                "Email və düzgün team rolu daxil edilməlidir.",
                CompanyTeamErrorCodes.Validation);
        }

        var access = await _companyAccessService.ResolveAsync(
            request.OwnerUserId,
            cancellationToken);

        if (access is null || !access.CanInvite)
        {
            return Failed(
                "Yalnız Founder və ya HR Admin team üzvü dəvət edə bilər.",
                CompanyTeamErrorCodes.Forbidden);
        }

        var owner = await _dbContext.Users
            .FirstOrDefaultAsync(
                item => item.Id == access.CompanyOwnerUserId,
                cancellationToken);

        if (owner is null)
        {
            return Failed(
                "Company owner tapılmadı.",
                CompanyTeamErrorCodes.NotFound);
        }

        if (!string.Equals(
                owner.AccountType,
                "employer",
                StringComparison.OrdinalIgnoreCase))
        {
            return Failed(
                "Yalnız employer hesabı team üzvü dəvət edə bilər.",
                CompanyTeamErrorCodes.Conflict);
        }

        var selectedRole = await ResolveRoleAsync(
            access.CompanyOwnerUserId,
            request.RoleId,
            request.Role,
            access.ActorUserId,
            cancellationToken);

        if (selectedRole is null)
        {
            return Failed(
                "Seçilən rol bu şirkətdə tapılmadı.",
                CompanyTeamErrorCodes.Validation);
        }

        if (string.Equals(
                owner.Email,
                request.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            return Failed(
                "Öz email ünvanınızı team-ə dəvət edə bilməzsiniz.",
                CompanyTeamErrorCodes.Conflict);
        }

        var registeredEmail = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                item => item.Email.ToLower() == request.Email,
                cancellationToken);

        if (registeredEmail)
        {
            return Failed(
                "Bu email ilə artıq qeydiyyatdan keçilib.",
                CompanyTeamErrorCodes.DuplicateEmail);
        }

        var existingOtherInvitation =
            await _dbContext.CompanyTeamInvitations
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.OwnerUserId != access.CompanyOwnerUserId
                        && item.Email == request.Email
                        && item.Status
                            == CompanyTeamInvitationStatuses.Invited
                        && item.ExpiresAtUtc > UtcNow(),
                    cancellationToken);

        if (existingOtherInvitation)
        {
            return Failed(
                "Bu email üçün başqa company invitation artıq aktivdir.",
                CompanyTeamErrorCodes.Conflict);
        }

        var invitation =
            await _dbContext.CompanyTeamInvitations
                .Include(item => item.AccessRole)
                .FirstOrDefaultAsync(
                    item =>
                        item.OwnerUserId == access.CompanyOwnerUserId
                        && item.Email == request.Email,
                    cancellationToken);

        if (invitation?.Status
            == CompanyTeamInvitationStatuses.Active)
        {
            return Failed(
                "Bu istifadəçi artıq team üzvüdür.",
                CompanyTeamErrorCodes.AlreadyAccepted);
        }

        var isNew = invitation is null;
        invitation ??= new CompanyTeamInvitation
        {
            Id = Guid.NewGuid(),
            OwnerUserId = access.CompanyOwnerUserId,
            Email = request.Email,
            CreatedAtUtc = UtcNow()
        };

        var previousRole = invitation.Role;
        var previousAccessRoleId = invitation.AccessRoleId;
        var previousStatus = invitation.Status;
        var previousTokenHash = invitation.TokenHash;
        var previousExpiry = invitation.ExpiresAtUtc;
        var previousSentAt = invitation.SentAtUtc;
        var previousUpdatedAt = invitation.UpdatedAtUtc;
        var previousAcceptedUserId = invitation.AcceptedUserId;
        var previousAcceptedAt = invitation.AcceptedAtUtc;

        var now = UtcNow();
        var token = TeamInvitationToken.Create();
        var lifetimeDays = Math.Clamp(
            _options.LifetimeDays,
            1,
            30);
        var expiresAtUtc = now.AddDays(lifetimeDays);

        invitation.Role = selectedRole.Name;
        invitation.AccessRoleId = selectedRole.Id;
        invitation.Status = CompanyTeamInvitationStatuses.Invited;
        invitation.TokenHash = TeamInvitationToken.Hash(token);
        invitation.ExpiresAtUtc = expiresAtUtc;
        invitation.SentAtUtc = now;
        invitation.UpdatedAtUtc = now;
        invitation.AcceptedUserId = null;
        invitation.AcceptedAtUtc = null;

        if (isNew)
            _dbContext.CompanyTeamInvitations.Add(invitation);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var companyName = GetCompanyName(owner);

        try
        {
            var invitationUrl = BuildInvitationUrl(token);

            await _emailSender.SendTeamInvitationAsync(
                request.Email,
                companyName,
                selectedRole.Name,
                invitationUrl,
                expiresAtUtc,
                cancellationToken);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Team invitation {InvitationId} email sorğusunun vaxtı bitdi.",
                invitation.Id);

            await RestoreInvitationAfterDeliveryFailureAsync(
                invitation,
                isNew,
                previousRole,
                previousAccessRoleId,
                previousStatus,
                previousTokenHash,
                previousExpiry,
                previousSentAt,
                previousUpdatedAt,
                previousAcceptedUserId,
                previousAcceptedAt);

            return Failed(
                "Invitation email göndərilmədi. Outlook/Microsoft Graph konfiqurasiyasını yoxlayın.",
                CompanyTeamErrorCodes.EmailDeliveryFailed);
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Team invitation {InvitationId} email-ə göndərilmədi.",
                invitation.Id);

            await RestoreInvitationAfterDeliveryFailureAsync(
                invitation,
                isNew,
                previousRole,
                previousAccessRoleId,
                previousStatus,
                previousTokenHash,
                previousExpiry,
                previousSentAt,
                previousUpdatedAt,
                previousAcceptedUserId,
                previousAcceptedAt);

            return Failed(
                "Invitation email göndərilmədi. Outlook/Microsoft Graph konfiqurasiyasını yoxlayın.",
                CompanyTeamErrorCodes.EmailDeliveryFailed);
        }

        AddAuditEvent(
            access.CompanyOwnerUserId,
            access.ActorUserId,
            invitation.AcceptedUserId,
            selectedRole.Id,
            CompanyAccessAuditEventTypes.AccessGranted,
            $"{invitation.Email} üçün {selectedRole.Name} access-i verildi",
            "Invitation göndərildi və rol təyin olundu.");
        await _dbContext.SaveChangesAsync(cancellationToken);
        invitation.AccessRole = selectedRole;

        return new CompanyTeamResponse
        {
            Success = true,
            Message = "Invitation email göndərildi.",
            CompanyName = companyName,
            CanManageTeam = true,
            Member = ToMemberDto(invitation)
        };
    }

    public async Task<CompanyTeamResponse> RemoveMemberAsync(
        Guid invitationId,
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        if (invitationId == Guid.Empty || actorUserId <= 0)
        {
            return Failed(
                "Team üzvü və istifadəçi məlumatı düzgün deyil.",
                CompanyTeamErrorCodes.Validation);
        }

        var invitation =
            await _dbContext.CompanyTeamInvitations
                .Include(item => item.AccessRole)
                .FirstOrDefaultAsync(
                    item => item.Id == invitationId
                        && item.Status
                            != CompanyTeamInvitationStatuses.Removed,
                    cancellationToken);

        if (invitation is null)
        {
            return Failed(
                "Team üzvü və ya invitation tapılmadı.",
                CompanyTeamErrorCodes.NotFound);
        }

        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);

        if (access is null
            || !access.CanDeactivate
            || access.CompanyOwnerUserId != invitation.OwnerUserId)
        {
            return Failed(
                "Yalnız Founder və ya həmin company-nin HR Admin-i team üzvünü silə bilər.",
                CompanyTeamErrorCodes.Forbidden);
        }

        if (!access.IsFounder
            && invitation.AccessRole?.IsSystem == true)
        {
            return Failed(
                "HR Admin başqa HR Admin-i silə bilməz.",
                CompanyTeamErrorCodes.Forbidden);
        }

        var pendingRegistrations =
            await _dbContext.PendingEmailRegistrations
                .Where(item =>
                    item.TeamInvitationId == invitation.Id)
                .ToListAsync(cancellationToken);

        if (pendingRegistrations.Count > 0)
        {
            _dbContext.PendingEmailRegistrations.RemoveRange(
                pendingRegistrations);
        }

        var now = UtcNow();
        invitation.Status =
            CompanyTeamInvitationStatuses.Removed;
        invitation.TokenHash = TeamInvitationToken.Hash(
            TeamInvitationToken.Create());
        invitation.ExpiresAtUtc = now;
        invitation.UpdatedAtUtc = now;

        AddAuditEvent(
            invitation.OwnerUserId,
            actorUserId,
            invitation.AcceptedUserId,
            invitation.AccessRoleId,
            CompanyAccessAuditEventTypes.AccessRevoked,
            $"{invitation.Email} istifadəçisinin access-i götürüldü",
            $"Əvvəlki rol: {invitation.Role}.");

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CompanyTeamResponse
        {
            Success = true,
            Message = "Team üzvü silindi.",
            CanManageTeam = true
        };
    }

    public async Task<CompanyTeamResponse> UpdateMemberRoleAsync(
        Guid invitationId,
        UpdateCompanyTeamMemberRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (invitationId == Guid.Empty || request.ActorUserId <= 0)
        {
            return Failed(
                "Team üzvü və istifadəçi məlumatı düzgün deyil.",
                CompanyTeamErrorCodes.Validation);
        }

        var invitation = await _dbContext.CompanyTeamInvitations
            .Include(item => item.AcceptedUser)
            .Include(item => item.OwnerUser)
            .Include(item => item.AccessRole)
            .FirstOrDefaultAsync(
                item => item.Id == invitationId
                    && item.Status != CompanyTeamInvitationStatuses.Removed,
                cancellationToken);

        if (invitation is null)
            return Failed("Team üzvü tapılmadı.", CompanyTeamErrorCodes.NotFound);

        var access = await _companyAccessService.ResolveAsync(
            request.ActorUserId,
            cancellationToken);

        if (access is null
            || !access.CanAssignRoles
            || access.CompanyOwnerUserId != invitation.OwnerUserId)
        {
            return Failed(
                "Bu team üzvünün access level-ini dəyişmək icazəniz yoxdur.",
                CompanyTeamErrorCodes.Forbidden);
        }

        if (!CanChangeRole(access, invitation))
        {
            return Failed(
                "Bu istifadəçinin rolunu dəyişmək icazəniz yoxdur.",
                CompanyTeamErrorCodes.Forbidden);
        }

        var requestedRole = await ResolveRoleAsync(
            invitation.OwnerUserId,
            request.RoleId,
            request.Role,
            request.ActorUserId,
            cancellationToken);
        if (requestedRole is null)
        {
            return Failed(
                "Seçilən rol bu şirkətdə tapılmadı.",
                CompanyTeamErrorCodes.Validation);
        }

        var previousRoleName = invitation.AccessRole?.Name ?? invitation.Role;
        var previousRoleId = invitation.AccessRoleId;
        invitation.Role = requestedRole.Name;
        invitation.AccessRoleId = requestedRole.Id;
        invitation.AccessRole = requestedRole;
        invitation.UpdatedAtUtc = UtcNow();

        if (previousRoleId != requestedRole.Id)
        {
            AddAuditEvent(
                invitation.OwnerUserId,
                request.ActorUserId,
                invitation.AcceptedUserId,
                requestedRole.Id,
                CompanyAccessAuditEventTypes.AccessChanged,
                $"{invitation.Email} üçün rol {requestedRole.Name} olaraq dəyişdirildi",
                $"Əvvəlki rol: {previousRoleName}. Yeni rol: {requestedRole.Name}.");
        }
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CompanyTeamResponse
        {
            Success = true,
            Message = "Access level yeniləndi.",
            CompanyName = GetCompanyName(invitation.OwnerUser),
            CanManageTeam = true,
            ActorRole = access.Role,
            Member = ToMemberDto(
                invitation,
                CanChangeRole(access, invitation),
                CanRemoveMember(access, invitation),
                (await LoadRolesAsync(
                    invitation.OwnerUserId,
                    cancellationToken)).Select(role => role.Name).ToList())
        };
    }

    public async Task<CompanyTeamResponse> CreateRoleAsync(
        SaveCompanyAccessRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await ResolveRoleManagementAccessAsync(
            request,
            cancellationToken);
        if (access is null)
        {
            return Failed(
                "Rol yaratmaq üçün Manage roles icazəsi lazımdır.",
                CompanyTeamErrorCodes.Forbidden);
        }

        var validation = ValidateRoleRequest(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return Failed(validation, CompanyTeamErrorCodes.Validation);

        await EnsureDefaultRoleAsync(
            access.CompanyOwnerUserId,
            access.ActorUserId,
            cancellationToken);

        var name = request.Name.Trim();
        var duplicate = await _dbContext.CompanyAccessRoles
            .AnyAsync(
                role => role.OwnerUserId == access.CompanyOwnerUserId
                    && role.Name.ToLower() == name.ToLower(),
                cancellationToken);
        if (duplicate)
        {
            return Failed(
                "Bu adda rol artıq mövcuddur.",
                CompanyTeamErrorCodes.Conflict);
        }

        var now = UtcNow();
        var role = new CompanyAccessRole
        {
            Id = Guid.NewGuid(),
            OwnerUserId = access.CompanyOwnerUserId,
            Name = name,
            Description = request.Description.Trim(),
            Scope = request.Scope.Trim().ToLowerInvariant(),
            IsSystem = false,
            IsFullAccess = false,
            CreatedByUserId = request.ActorUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var permissionKey in NormalizePermissionKeys(request.PermissionKeys))
        {
            role.Permissions.Add(new CompanyAccessRolePermission
            {
                RoleId = role.Id,
                PermissionKey = permissionKey
            });
        }

        _dbContext.CompanyAccessRoles.Add(role);
        AddAuditEvent(
            access.CompanyOwnerUserId,
            access.ActorUserId,
            null,
            role.Id,
            CompanyAccessAuditEventTypes.RoleCreated,
            $"{role.Name} rolu yaradıldı",
            $"Scope: {role.Scope}. Access sayı: {role.Permissions.Count}.");
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildManagementResponseAsync(
            access,
            "Rol yaradıldı.",
            cancellationToken);
    }

    public async Task<CompanyTeamResponse> UpdateRoleAsync(
        Guid roleId,
        SaveCompanyAccessRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (roleId == Guid.Empty)
            return Failed("Rol ID düzgün deyil.", CompanyTeamErrorCodes.Validation);

        var access = await ResolveRoleManagementAccessAsync(
            request,
            cancellationToken);
        if (access is null)
        {
            return Failed(
                "Rolu dəyişmək üçün Manage roles icazəsi lazımdır.",
                CompanyTeamErrorCodes.Forbidden);
        }

        var validation = ValidateRoleRequest(request);
        if (!string.IsNullOrWhiteSpace(validation))
            return Failed(validation, CompanyTeamErrorCodes.Validation);

        var role = await _dbContext.CompanyAccessRoles
            .Include(item => item.Permissions)
            .FirstOrDefaultAsync(
                item => item.Id == roleId
                    && item.OwnerUserId == access.CompanyOwnerUserId,
                cancellationToken);
        if (role is null)
            return Failed("Rol tapılmadı.", CompanyTeamErrorCodes.NotFound);
        if (role.IsSystem)
        {
            return Failed(
                "HR Admin sistem roludur və dəyişdirilə bilməz.",
                CompanyTeamErrorCodes.Forbidden);
        }

        var name = request.Name.Trim();
        var duplicate = await _dbContext.CompanyAccessRoles.AnyAsync(
            item => item.OwnerUserId == access.CompanyOwnerUserId
                && item.Id != roleId
                && item.Name.ToLower() == name.ToLower(),
            cancellationToken);
        if (duplicate)
        {
            return Failed(
                "Bu adda rol artıq mövcuddur.",
                CompanyTeamErrorCodes.Conflict);
        }

        var previousName = role.Name;
        var requestedKeys = NormalizePermissionKeys(request.PermissionKeys);
        var existingKeys = role.Permissions
            .Select(item => item.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var granted = requestedKeys.Except(
            existingKeys,
            StringComparer.OrdinalIgnoreCase).ToList();
        var revoked = existingKeys.Except(
            requestedKeys,
            StringComparer.OrdinalIgnoreCase).ToList();

        role.Name = name;
        role.Description = request.Description.Trim();
        role.Scope = request.Scope.Trim().ToLowerInvariant();
        role.UpdatedAtUtc = UtcNow();

        if (revoked.Count > 0)
        {
            var revokedRows = role.Permissions
                .Where(item => revoked.Contains(
                    item.PermissionKey,
                    StringComparer.OrdinalIgnoreCase))
                .ToList();
            _dbContext.CompanyAccessRolePermissions.RemoveRange(revokedRows);
        }

        foreach (var permissionKey in granted)
        {
            role.Permissions.Add(new CompanyAccessRolePermission
            {
                RoleId = role.Id,
                PermissionKey = permissionKey
            });
            AddPermissionAudit(
                access,
                role,
                permissionKey,
                granted: true);
        }

        foreach (var permissionKey in revoked)
        {
            AddPermissionAudit(
                access,
                role,
                permissionKey,
                granted: false);
        }

        AddAuditEvent(
            access.CompanyOwnerUserId,
            access.ActorUserId,
            null,
            role.Id,
            CompanyAccessAuditEventTypes.RoleUpdated,
            $"{role.Name} rolu yeniləndi",
            $"Əvvəlki ad: {previousName}. Verilən access: {granted.Count}. Götürülən access: {revoked.Count}.");
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildManagementResponseAsync(
            access,
            "Rol yeniləndi.",
            cancellationToken);
    }

    private async Task RestoreInvitationAfterDeliveryFailureAsync(
        CompanyTeamInvitation invitation,
        bool isNew,
        string previousRole,
        Guid? previousAccessRoleId,
        string previousStatus,
        string previousTokenHash,
        DateTime previousExpiry,
        DateTime previousSentAt,
        DateTime previousUpdatedAt,
        int? previousAcceptedUserId,
        DateTime? previousAcceptedAt)
    {
        if (isNew)
        {
            _dbContext.CompanyTeamInvitations.Remove(invitation);
        }
        else
        {
            invitation.Role = previousRole;
            invitation.AccessRoleId = previousAccessRoleId;
            invitation.Status = previousStatus;
            invitation.TokenHash = previousTokenHash;
            invitation.ExpiresAtUtc = previousExpiry;
            invitation.SentAtUtc = previousSentAt;
            invitation.UpdatedAtUtc = previousUpdatedAt;
            invitation.AcceptedUserId = previousAcceptedUserId;
            invitation.AcceptedAtUtc = previousAcceptedAt;
        }

        await _dbContext.SaveChangesAsync(
            CancellationToken.None);
    }

    public async Task<ResolveCompanyTeamInvitationResponse>
        ResolveInvitationAsync(
            string token,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ResolveFailed(
                "Invitation link düzgün deyil.",
                CompanyTeamErrorCodes.Validation);
        }

        var tokenHash =
            TeamInvitationToken.Hash(token);
        var invitation =
            await _dbContext.CompanyTeamInvitations
                .AsNoTracking()
                .Include(item => item.OwnerUser)
                .Include(item => item.AccessRole)
                .FirstOrDefaultAsync(
                    item => item.TokenHash == tokenHash,
                    cancellationToken);

        if (invitation is null)
        {
            return ResolveFailed(
                "Invitation tapılmadı.",
                CompanyTeamErrorCodes.NotFound);
        }

        if (invitation.Status
            == CompanyTeamInvitationStatuses.Active)
        {
            return ResolveFailed(
                "Bu invitation artıq qəbul edilib.",
                CompanyTeamErrorCodes.AlreadyAccepted);
        }

        if (invitation.Status
            == CompanyTeamInvitationStatuses.Removed)
        {
            return ResolveFailed(
                "Bu invitation ləğv edilib.",
                CompanyTeamErrorCodes.Expired);
        }

        if (invitation.ExpiresAtUtc <= UtcNow())
        {
            return ResolveFailed(
                "Invitation link-in vaxtı bitib. Company admin-dən yenisini istəyin.",
                CompanyTeamErrorCodes.Expired);
        }

        return new ResolveCompanyTeamInvitationResponse
        {
            Success = true,
            Message = "Invitation etibarlıdır.",
            Email = invitation.Email,
            Role = invitation.AccessRole?.Name ?? invitation.Role,
            CompanyName = GetCompanyName(invitation.OwnerUser),
            CompanyType = invitation.OwnerUser.CompanyType,
            Industry = invitation.OwnerUser.Industry,
            ExpiresAtUtc = invitation.ExpiresAtUtc
        };
    }

    private string BuildInvitationUrl(string token)
    {
        var baseUrl =
            (_options.WebAppBaseUrl ?? string.Empty)
                .Trim()
                .TrimEnd('/');

        if (!Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out _))
        {
            throw new InvalidOperationException(
                "TeamInvitations:WebAppBaseUrl düzgün absolute URL deyil.");
        }

        return $"{baseUrl}/Registration?invite={Uri.EscapeDataString(token)}";
    }

    private DateTime UtcNow()
    {
        return _timeProvider
            .GetUtcNow()
            .UtcDateTime;
    }

    private static string GetCompanyName(User owner)
    {
        if (!string.IsNullOrWhiteSpace(owner.CompanyName))
            return owner.CompanyName;

        if (!string.IsNullOrWhiteSpace(owner.Name))
            return owner.Name;

        return owner.Email;
    }

    private static CompanyTeamMemberDto ToMemberDto(
        CompanyTeamInvitation invitation,
        bool canChangeRole = false,
        bool canRemove = false,
        IReadOnlyCollection<string>? allowedRoles = null)
    {
        var activeName =
            invitation.AcceptedUser is null
                ? string.Empty
                : string.Join(
                    " ",
                    new[]
                    {
                        invitation.AcceptedUser.Name,
                        invitation.AcceptedUser.Surname
                    }.Where(
                        value =>
                            !string.IsNullOrWhiteSpace(value)));

        return new CompanyTeamMemberDto
        {
            InvitationId = invitation.Id,
            UserId = invitation.AcceptedUserId,
            DisplayName =
                string.IsNullOrWhiteSpace(activeName)
                    ? invitation.Email
                    : activeName,
            Email = invitation.Email,
            Role = invitation.AccessRole?.Name ?? invitation.Role,
            RoleId = invitation.AccessRoleId,
            Scope = invitation.AccessRole?.Scope
                ?? CompanyAccessRoleScopes.Company,
            Status = invitation.Status,
            InvitedAtUtc = invitation.SentAtUtc,
            AcceptedAtUtc = invitation.AcceptedAtUtc,
            CanChangeRole = canChangeRole,
            CanRemove = canRemove,
            AllowedRoles = allowedRoles?.ToList() ?? []
        };
    }

    private static bool CanChangeRole(
        CompanyAccessContext access,
        CompanyTeamInvitation invitation)
    {
        if (access.IsFounder)
            return true;

        return access.CanAssignRoles
            && invitation.AccessRole?.IsSystem != true;
    }

    private static bool CanRemoveMember(
        CompanyAccessContext access,
        CompanyTeamInvitation invitation)
    {
        return access.CanDeactivate
            && (access.IsFounder || invitation.AccessRole?.IsSystem != true);
    }

    private static CompanyTeamMemberDto ToFounderMemberDto(
        User owner,
        CompanyAccessRole defaultRole)
    {
        var displayName = string.Join(
            " ",
            new[] { owner.Name, owner.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return new CompanyTeamMemberDto
        {
            InvitationId = Guid.Empty,
            UserId = owner.Id,
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? owner.Email
                : displayName,
            Email = owner.Email,
            Role = defaultRole.Name,
            RoleId = defaultRole.Id,
            Scope = defaultRole.Scope,
            Status = CompanyTeamInvitationStatuses.Active,
            InvitedAtUtc = owner.CreatedAt,
            AcceptedAtUtc = owner.CreatedAt,
            IsFounder = true
        };
    }

    private async Task<CompanyAccessContext?> ResolveRoleManagementAccessAsync(
        SaveCompanyAccessRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ActorUserId <= 0)
            return null;

        var access = await _companyAccessService.ResolveAsync(
            request.ActorUserId,
            cancellationToken);
        return access?.CanManageRoles == true ? access : null;
    }

    private static string ValidateRoleRequest(
        SaveCompanyAccessRoleRequest request)
    {
        request.Name = request.Name?.Trim() ?? string.Empty;
        request.Description = request.Description?.Trim() ?? string.Empty;
        request.Scope = request.Scope?.Trim().ToLowerInvariant() ?? string.Empty;
        request.PermissionKeys ??= [];

        if (request.Name.Length is < 2 or > 80)
            return "Rol adı 2-80 simvol arasında olmalıdır.";
        if (!CompanyAccessRoleScopes.All.Contains(request.Scope))
            return "Scope düzgün seçilməyib.";
        if (request.PermissionKeys.Any(
            key => !CompanyAccessPermissionCatalog.AllKeys.Contains(key)))
        {
            return "Access siyahısında naməlum permission var.";
        }

        return string.Empty;
    }

    private static List<string> NormalizePermissionKeys(
        IEnumerable<string>? permissionKeys)
    {
        return (permissionKeys ?? [])
            .Select(key => key?.Trim() ?? string.Empty)
            .Where(CompanyAccessPermissionCatalog.AllKeys.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(key => key)
            .ToList();
    }

    private async Task<CompanyAccessRole> EnsureDefaultRoleAsync(
        int companyOwnerUserId,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var role = await _dbContext.CompanyAccessRoles
            .Include(item => item.Permissions)
            .FirstOrDefaultAsync(
                item => item.OwnerUserId == companyOwnerUserId
                    && (item.IsSystem || item.Name == "HR Admin"),
                cancellationToken);
        var now = UtcNow();
        var created = role is null;

        if (role is null)
        {
            role = new CompanyAccessRole
            {
                Id = Guid.NewGuid(),
                OwnerUserId = companyOwnerUserId,
                Name = "HR Admin",
                Description = "Full access",
                Scope = CompanyAccessRoleScopes.Company,
                IsSystem = true,
                IsFullAccess = true,
                CreatedByUserId = actorUserId,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            _dbContext.CompanyAccessRoles.Add(role);
        }
        else
        {
            role.Name = "HR Admin";
            role.Description = "Full access";
            role.Scope = CompanyAccessRoleScopes.Company;
            role.IsSystem = true;
            role.IsFullAccess = true;
            role.UpdatedAtUtc = now;
        }

        var existingKeys = role.Permissions
            .Select(item => item.PermissionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var key in CompanyAccessPermissionCatalog.AllKeys)
        {
            if (existingKeys.Contains(key))
                continue;
            role.Permissions.Add(new CompanyAccessRolePermission
            {
                RoleId = role.Id,
                PermissionKey = key
            });
        }

        var legacyHrAdmins = await _dbContext.CompanyTeamInvitations
            .Where(item => item.OwnerUserId == companyOwnerUserId
                && !item.AccessRoleId.HasValue
                && item.Role == "HR Admin")
            .ToListAsync(cancellationToken);
        foreach (var invitation in legacyHrAdmins)
            invitation.AccessRoleId = role.Id;

        if (created)
        {
            AddAuditEvent(
                companyOwnerUserId,
                actorUserId,
                null,
                role.Id,
                CompanyAccessAuditEventTypes.RoleCreated,
                "HR Admin sistem rolu yaradıldı",
                "Full access. Scope: the whole company.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return role;
    }

    private async Task<CompanyAccessRole?> ResolveRoleAsync(
        int companyOwnerUserId,
        Guid? roleId,
        string? legacyRoleName,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        await EnsureDefaultRoleAsync(
            companyOwnerUserId,
            actorUserId,
            cancellationToken);

        var normalizedName = legacyRoleName?.Trim() ?? string.Empty;
        return await _dbContext.CompanyAccessRoles
            .Include(item => item.Permissions)
            .FirstOrDefaultAsync(
                item => item.OwnerUserId == companyOwnerUserId
                    && (roleId.HasValue
                        ? item.Id == roleId.Value
                        : item.Name == normalizedName),
                cancellationToken);
    }

    private async Task<List<CompanyAccessRoleDto>> LoadRolesAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.CompanyAccessRoles
            .AsNoTracking()
            .Include(item => item.Permissions)
            .Where(item => item.OwnerUserId == companyOwnerUserId)
            .OrderByDescending(item => item.IsSystem)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var counts = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Where(item => item.OwnerUserId == companyOwnerUserId
                && item.Status != CompanyTeamInvitationStatuses.Removed
                && item.AccessRoleId.HasValue)
            .GroupBy(item => item.AccessRoleId!.Value)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);

        return roles.Select(role => new CompanyAccessRoleDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Scope = role.Scope,
            IsSystem = role.IsSystem,
            IsFullAccess = role.IsFullAccess,
            ParticipantCount = counts.GetValueOrDefault(role.Id)
                + (role.IsSystem ? 1 : 0),
            PermissionKeys = role.Permissions
                .Select(item => item.PermissionKey)
                .OrderBy(key => key)
                .ToList()
        }).ToList();
    }

    private async Task<List<CompanyAccessAuditEventDto>> LoadHistoryAsync(
        int companyOwnerUserId,
        CancellationToken cancellationToken)
    {
        var events = await _dbContext.CompanyAccessAuditEvents
            .AsNoTracking()
            .Where(item => item.OwnerUserId == companyOwnerUserId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(500)
            .ToListAsync(cancellationToken);

        var userIds = events
            .SelectMany(item => new int?[] { item.ActorUserId, item.TargetUserId })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToList();
        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var roleIds = events
            .Where(item => item.RoleId.HasValue)
            .Select(item => item.RoleId!.Value)
            .Distinct()
            .ToList();
        var roles = await _dbContext.CompanyAccessRoles
            .AsNoTracking()
            .Where(item => roleIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        return events.Select(item =>
        {
            users.TryGetValue(item.ActorUserId, out var actor);
            User? target = null;
            if (item.TargetUserId.HasValue)
                users.TryGetValue(item.TargetUserId.Value, out target);
            CompanyAccessRole? role = null;
            if (item.RoleId.HasValue)
                roles.TryGetValue(item.RoleId.Value, out role);

            return new CompanyAccessAuditEventDto
            {
                Id = item.Id,
                EventType = item.EventType,
                Summary = item.Summary,
                Details = item.Details,
                ActorUserId = item.ActorUserId,
                ActorName = UserDisplayName(actor),
                ActorEmail = actor?.Email ?? string.Empty,
                TargetUserId = item.TargetUserId,
                TargetName = UserDisplayName(target),
                TargetEmail = target?.Email ?? string.Empty,
                RoleId = item.RoleId,
                RoleName = role?.Name ?? string.Empty,
                CreatedAtUtc = item.CreatedAtUtc
            };
        }).ToList();
    }

    private static List<CompanyPermissionGroupDto> BuildPermissionGroups()
    {
        return CompanyAccessPermissionCatalog.Groups.Select(group =>
            new CompanyPermissionGroupDto
            {
                Key = group.Key,
                Label = group.Label,
                Permissions = group.Permissions.Select(permission =>
                    new CompanyPermissionDto
                    {
                        Key = permission.Key,
                        Label = permission.Label,
                        Sensitive = permission.Sensitive
                    }).ToList()
            }).ToList();
    }

    private async Task<CompanyTeamResponse> BuildManagementResponseAsync(
        CompanyAccessContext access,
        string message,
        CancellationToken cancellationToken)
    {
        return new CompanyTeamResponse
        {
            Success = true,
            Message = message,
            CanManageTeam = access.CanManageTeam,
            CanManageRoles = access.CanManageRoles,
            CanInvite = access.CanInvite,
            ActorRole = access.Role,
            Roles = await LoadRolesAsync(
                access.CompanyOwnerUserId,
                cancellationToken),
            History = await LoadHistoryAsync(
                access.CompanyOwnerUserId,
                cancellationToken),
            PermissionGroups = BuildPermissionGroups()
        };
    }

    private void AddPermissionAudit(
        CompanyAccessContext access,
        CompanyAccessRole role,
        string permissionKey,
        bool granted)
    {
        var permission = CompanyAccessPermissionCatalog.ByKey[permissionKey];
        AddAuditEvent(
            access.CompanyOwnerUserId,
            access.ActorUserId,
            null,
            role.Id,
            granted
                ? CompanyAccessAuditEventTypes.PermissionGranted
                : CompanyAccessAuditEventTypes.PermissionRevoked,
            granted
                ? $"{role.Name} roluna access verildi: {permission.Label}"
                : $"{role.Name} rolundan access götürüldü: {permission.Label}",
            $"Permission key: {permission.Key}.");
    }

    private void AddAuditEvent(
        int companyOwnerUserId,
        int actorUserId,
        int? targetUserId,
        Guid? roleId,
        string eventType,
        string summary,
        string details)
    {
        _dbContext.CompanyAccessAuditEvents.Add(new CompanyAccessAuditEvent
        {
            Id = Guid.NewGuid(),
            OwnerUserId = companyOwnerUserId,
            ActorUserId = actorUserId,
            TargetUserId = targetUserId,
            RoleId = roleId,
            EventType = eventType,
            Summary = summary,
            Details = details,
            CreatedAtUtc = UtcNow()
        });
    }

    private static string UserDisplayName(User? user)
    {
        if (user is null)
            return string.Empty;
        var name = string.Join(
            " ",
            new[] { user.Name, user.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    private static CompanyTeamResponse Failed(
        string message,
        string errorCode)
    {
        return new CompanyTeamResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    private static ResolveCompanyTeamInvitationResponse
        ResolveFailed(
            string message,
            string errorCode)
    {
        return new ResolveCompanyTeamInvitationResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

}
