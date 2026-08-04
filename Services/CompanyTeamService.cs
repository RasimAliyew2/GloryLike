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
    private readonly TeamInvitationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<CompanyTeamService> _logger;

    public CompanyTeamService(
        AppDbContext dbContext,
        IRegistrationEmailSender emailSender,
        IOptions<TeamInvitationOptions> options,
        TimeProvider timeProvider,
        ILogger<CompanyTeamService> logger)
    {
        _dbContext = dbContext;
        _emailSender = emailSender;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<CompanyTeamResponse> GetTeamAsync(
        int ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var owner = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == ownerUserId,
                cancellationToken);

        if (owner is null)
        {
            return Failed(
                "Company owner tapılmadı.",
                CompanyTeamErrorCodes.NotFound);
        }

        var invitations = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Include(item => item.AcceptedUser)
            .Where(item => item.OwnerUserId == ownerUserId)
            .OrderBy(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return new CompanyTeamResponse
        {
            Success = true,
            CompanyName = GetCompanyName(owner),
            Members = invitations
                .Select(ToMemberDto)
                .ToList()
        };
    }

    public async Task<CompanyTeamResponse> InviteAsync(
        InviteCompanyTeamMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        request.Email =
            request.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;
        request.Role = NormalizeRole(request.Role);

        if (request.OwnerUserId <= 0
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Role))
        {
            return Failed(
                "Email və düzgün team rolu daxil edilməlidir.",
                CompanyTeamErrorCodes.Validation);
        }

        var owner = await _dbContext.Users
            .FirstOrDefaultAsync(
                item => item.Id == request.OwnerUserId,
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
                        item.OwnerUserId != request.OwnerUserId
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
                .FirstOrDefaultAsync(
                    item =>
                        item.OwnerUserId == request.OwnerUserId
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
            OwnerUserId = request.OwnerUserId,
            Email = request.Email,
            CreatedAtUtc = UtcNow()
        };

        var previousRole = invitation.Role;
        var previousTokenHash = invitation.TokenHash;
        var previousExpiry = invitation.ExpiresAtUtc;
        var previousSentAt = invitation.SentAtUtc;
        var previousUpdatedAt = invitation.UpdatedAtUtc;

        var now = UtcNow();
        var token = TeamInvitationToken.Create();
        var lifetimeDays = Math.Clamp(
            _options.LifetimeDays,
            1,
            30);
        var expiresAtUtc = now.AddDays(lifetimeDays);

        invitation.Role = request.Role;
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
                request.Role,
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
                previousTokenHash,
                previousExpiry,
                previousSentAt,
                previousUpdatedAt);

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
                previousTokenHash,
                previousExpiry,
                previousSentAt,
                previousUpdatedAt);

            return Failed(
                "Invitation email göndərilmədi. Outlook/Microsoft Graph konfiqurasiyasını yoxlayın.",
                CompanyTeamErrorCodes.EmailDeliveryFailed);
        }

        return new CompanyTeamResponse
        {
            Success = true,
            Message = "Invitation email göndərildi.",
            CompanyName = companyName,
            Member = ToMemberDto(invitation)
        };
    }

    private async Task RestoreInvitationAfterDeliveryFailureAsync(
        CompanyTeamInvitation invitation,
        bool isNew,
        string previousRole,
        string previousTokenHash,
        DateTime previousExpiry,
        DateTime previousSentAt,
        DateTime previousUpdatedAt)
    {
        if (isNew)
        {
            _dbContext.CompanyTeamInvitations.Remove(invitation);
        }
        else
        {
            invitation.Role = previousRole;
            invitation.TokenHash = previousTokenHash;
            invitation.ExpiresAtUtc = previousExpiry;
            invitation.SentAtUtc = previousSentAt;
            invitation.UpdatedAtUtc = previousUpdatedAt;
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
            Role = invitation.Role,
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

    private static string NormalizeRole(string? role)
    {
        return role?.Trim().ToLowerInvariant() switch
        {
            "hr admin" => "HR Admin",
            "hiring manager" => "Hiring Manager",
            "recruiter" => "Recruiter",
            _ => string.Empty
        };
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
        CompanyTeamInvitation invitation)
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
            Role = invitation.Role,
            Status = invitation.Status,
            InvitedAtUtc = invitation.SentAtUtc,
            AcceptedAtUtc = invitation.AcceptedAtUtc
        };
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
