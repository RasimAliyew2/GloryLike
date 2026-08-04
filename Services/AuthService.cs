using System.Security.Cryptography;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Auth;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Hash;
using GloryLikeBackend.Services.Interfaces;
using GloryLikeBackend.Services.Security;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public class AuthService : IAuthService
{
    private const int MaximumFailedVerificationAttempts = 5;
    private static readonly TimeSpan VerificationLifetime =
        TimeSpan.FromMinutes(1);

    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;
    private readonly IRegistrationEmailSender _registrationEmailSender;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext dbContext,
        IWebHostEnvironment environment,
        IRegistrationEmailSender registrationEmailSender,
        TimeProvider timeProvider,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _environment = environment;
        _registrationEmailSender = registrationEmailSender;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        NormalizeRegisterRequest(request);

        var duplicate = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == request.Email.ToLower() ||
                x.PhoneNumber == request.PhoneNumber ||
                x.UserName.ToLower() == request.UserName.ToLower(),
                cancellationToken);

        if (duplicate is not null)
        {
            if (duplicate.Email.Equals(
                    request.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return Failed(
                    "Bu email ilə artıq qeydiyyatdan keçilib.");
            }

            if (duplicate.PhoneNumber == request.PhoneNumber)
            {
                return Failed(
                    "Bu telefon nömrəsi ilə artıq qeydiyyatdan keçilib.");
            }

            return Failed(
                "Bu username ilə artıq qeydiyyatdan keçilib.");
        }

        var now = UtcNow();
        var user = new User
        {
            UserName = request.UserName,
            Name = request.Name,
            Surname = request.Surname,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            AccountType = "candidate",
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Qeydiyyat tamamlandı.",
            User = ToDto(user)
        };
    }

    public async Task<EmailRegistrationResponse>
        StartEmailRegistrationAsync(
            StartEmailRegistrationRequest request,
            CancellationToken cancellationToken = default)
    {
        NormalizeEmailRegistrationRequest(request);

        CompanyTeamInvitation? teamInvitation = null;

        if (!string.IsNullOrWhiteSpace(request.InvitationToken))
        {
            var tokenHash = TeamInvitationToken.Hash(
                request.InvitationToken);

            teamInvitation = await _dbContext.CompanyTeamInvitations
                .Include(item => item.OwnerUser)
                .FirstOrDefaultAsync(
                    item => item.TokenHash == tokenHash,
                    cancellationToken);

            if (teamInvitation is null)
            {
                return EmailRegistrationFailed(
                    "Invitation tapılmadı.",
                    EmailRegistrationErrorCodes.NotFound);
            }

            if (teamInvitation.Status
                == CompanyTeamInvitationStatuses.Active)
            {
                return EmailRegistrationFailed(
                    "Bu invitation artıq qəbul edilib.",
                    EmailRegistrationErrorCodes.Conflict);
            }

            if (teamInvitation.ExpiresAtUtc <= UtcNow())
            {
                return EmailRegistrationFailed(
                    "Invitation link-in vaxtı bitib.",
                    EmailRegistrationErrorCodes.Expired);
            }

            if (!string.Equals(
                    teamInvitation.Email,
                    request.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return EmailRegistrationFailed(
                    "Qeydiyyat email-i invitation email-i ilə eyni olmalıdır.",
                    EmailRegistrationErrorCodes.Conflict);
            }

            request.AccountType = "employer";
            request.CompanyName =
                GetCompanyName(teamInvitation.OwnerUser);
            request.CompanyType =
                teamInvitation.OwnerUser.CompanyType;
            request.Industry =
                teamInvitation.OwnerUser.Industry;
        }

        var validationMessage =
            ValidateEmailRegistration(
                request,
                isTeamInvitation: teamInvitation is not null);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return EmailRegistrationFailed(
                validationMessage,
                EmailRegistrationErrorCodes.Validation);
        }

        var emailExists = await EmailAlreadyRegisteredAsync(
            request.Email,
            cancellationToken);

        if (emailExists)
        {
            return EmailRegistrationFailed(
                "Bu email ilə artıq qeydiyyatdan keçilib.",
                EmailRegistrationErrorCodes.DuplicateEmail);
        }

        var now = UtcNow();

        await _dbContext.PendingEmailRegistrations
            .Where(item => item.UpdatedAtUtc < now.AddDays(-1))
            .ExecuteDeleteAsync(cancellationToken);

        var pending = await _dbContext.PendingEmailRegistrations
            .FirstOrDefaultAsync(
                item => item.Email == request.Email,
                cancellationToken);

        if (pending is not null
            && pending.ResendAvailableAtUtc > now)
        {
            ApplyRegistrationData(
                pending,
                request,
                teamInvitation?.Id,
                now,
                updatePassword: true);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return BuildEmailRegistrationStatus(
                pending,
                now,
                true,
                "Təsdiq kodu artıq göndərilib. "
                + "Yeni kod üçün 1 dəqiqənin tamamlanmasını gözləyin.");
        }

        pending ??= new PendingEmailRegistration
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            CreatedAtUtc = now
        };

        ApplyRegistrationData(
            pending,
            request,
            teamInvitation?.Id,
            now,
            updatePassword: true);

        var verificationCode = CreateVerificationCode();
        ApplyNewVerificationCode(
            pending,
            verificationCode,
            now);

        if (_dbContext.Entry(pending).State
            == EntityState.Detached)
        {
            _dbContext.PendingEmailRegistrations.Add(pending);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var emailSent = await TrySendVerificationCodeAsync(
            pending,
            verificationCode,
            cancellationToken);

        if (!emailSent)
        {
            await MakeImmediateRetryAvailableAsync(
                pending,
                cancellationToken);

            return EmailRegistrationFailed(
                "Təsdiq kodu email-ə göndərilmədi. "
                + "Outlook/Microsoft Graph konfiqurasiyasını yoxlayın və yenidən cəhd edin.",
                EmailRegistrationErrorCodes.EmailDeliveryFailed,
                pending,
                UtcNow());
        }

        return BuildEmailRegistrationStatus(
            pending,
            now,
            true,
            "6 rəqəmli təsdiq kodu email ünvanınıza göndərildi.");
    }

    public async Task<EmailRegistrationResponse>
        GetEmailRegistrationStatusAsync(
            Guid verificationId,
            CancellationToken cancellationToken = default)
    {
        if (verificationId == Guid.Empty)
        {
            return EmailRegistrationFailed(
                "Verification ID düzgün deyil.",
                EmailRegistrationErrorCodes.Validation);
        }

        var pending = await _dbContext.PendingEmailRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == verificationId,
                cancellationToken);

        if (pending is null)
        {
            return EmailRegistrationFailed(
                "Qeydiyyat sorğusu tapılmadı. Yenidən qeydiyyata başlayın.",
                EmailRegistrationErrorCodes.NotFound);
        }

        return BuildEmailRegistrationStatus(
            pending,
            UtcNow(),
            true,
            "Təsdiq kodunu daxil edin.");
    }

    public async Task<EmailRegistrationResponse>
        VerifyEmailRegistrationAsync(
            VerifyEmailRegistrationRequest request,
            CancellationToken cancellationToken = default)
    {
        var normalizedCode = request.Code?.Trim() ?? string.Empty;

        if (request.VerificationId == Guid.Empty
            || normalizedCode.Length != 6
            || normalizedCode.Any(character => !char.IsDigit(character)))
        {
            return EmailRegistrationFailed(
                "Təsdiq kodu 6 rəqəmdən ibarət olmalıdır.",
                EmailRegistrationErrorCodes.Validation);
        }

        var pending = await _dbContext.PendingEmailRegistrations
            .FirstOrDefaultAsync(
                item => item.Id == request.VerificationId,
                cancellationToken);

        if (pending is null)
        {
            return EmailRegistrationFailed(
                "Qeydiyyat sorğusu tapılmadı. Yenidən qeydiyyata başlayın.",
                EmailRegistrationErrorCodes.NotFound);
        }

        var now = UtcNow();

        if (await EmailAlreadyRegisteredAsync(
                pending.Email,
                cancellationToken))
        {
            _dbContext.PendingEmailRegistrations.Remove(pending);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return EmailRegistrationFailed(
                "Bu email ilə artıq qeydiyyatdan keçilib.",
                EmailRegistrationErrorCodes.DuplicateEmail);
        }

        if (pending.VerificationCodeExpiresAtUtc <= now)
        {
            return EmailRegistrationFailed(
                "Təsdiq kodunun 1 dəqiqəlik vaxtı bitib. Yeni kod göndərin.",
                EmailRegistrationErrorCodes.Expired,
                pending,
                now);
        }

        if (pending.FailedAttemptCount
            >= MaximumFailedVerificationAttempts)
        {
            return EmailRegistrationFailed(
                "Çox sayda səhv kod daxil edilib. Vaxt bitdikdən sonra yeni kod göndərin.",
                EmailRegistrationErrorCodes.TooManyAttempts,
                pending,
                now);
        }

        var codeMatches = PasswordHasher.VerifyPassword(
            BuildCodeSecret(
                pending.Id,
                normalizedCode),
            pending.VerificationCodeHash);

        if (!codeMatches)
        {
            pending.FailedAttemptCount++;
            pending.UpdatedAtUtc = now;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return EmailRegistrationFailed(
                "Təsdiq kodu yanlışdır.",
                pending.FailedAttemptCount
                    >= MaximumFailedVerificationAttempts
                        ? EmailRegistrationErrorCodes.TooManyAttempts
                        : EmailRegistrationErrorCodes.InvalidCode,
                pending,
                now);
        }

        CompanyTeamInvitation? teamInvitation = null;

        if (pending.TeamInvitationId is Guid teamInvitationId)
        {
            teamInvitation = await _dbContext.CompanyTeamInvitations
                .Include(item => item.OwnerUser)
                .FirstOrDefaultAsync(
                    item => item.Id == teamInvitationId,
                    cancellationToken);

            if (teamInvitation is null)
            {
                return EmailRegistrationFailed(
                    "Invitation tapılmadı.",
                    EmailRegistrationErrorCodes.NotFound);
            }

            if (teamInvitation.Status
                == CompanyTeamInvitationStatuses.Active)
            {
                return EmailRegistrationFailed(
                    "Bu invitation artıq qəbul edilib.",
                    EmailRegistrationErrorCodes.Conflict);
            }

            if (teamInvitation.ExpiresAtUtc <= now)
            {
                return EmailRegistrationFailed(
                    "Invitation link-in vaxtı bitib.",
                    EmailRegistrationErrorCodes.Expired);
            }

            if (!string.Equals(
                    teamInvitation.Email,
                    pending.Email,
                    StringComparison.OrdinalIgnoreCase))
            {
                return EmailRegistrationFailed(
                    "Qeydiyyat email-i invitation email-i ilə eyni olmalıdır.",
                    EmailRegistrationErrorCodes.Conflict);
            }
        }

        var user = BuildVerifiedUser(
            pending,
            now,
            isCompanyTeamMember: teamInvitation is not null);

        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            _dbContext.Users.Add(user);

            if (teamInvitation is not null)
            {
                teamInvitation.AcceptedUser = user;
                teamInvitation.Status =
                    CompanyTeamInvitationStatuses.Active;
                teamInvitation.AcceptedAtUtc = now;
                teamInvitation.UpdatedAtUtc = now;
            }

            _dbContext.PendingEmailRegistrations.Remove(pending);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            _dbContext.ChangeTracker.Clear();

            _logger.LogWarning(
                exception,
                "Email verification zamanı {Email} üçün user yaradılmadı.",
                pending.Email);

            var duplicateEmail = await EmailAlreadyRegisteredAsync(
                pending.Email,
                cancellationToken);

            return EmailRegistrationFailed(
                duplicateEmail
                    ? "Bu email ilə artıq qeydiyyatdan keçilib."
                    : "Qeydiyyat SQL-də saxlanmadı. Yenidən cəhd edin.",
                duplicateEmail
                    ? EmailRegistrationErrorCodes.DuplicateEmail
                    : EmailRegistrationErrorCodes.Conflict);
        }

        return new EmailRegistrationResponse
        {
            Success = true,
            Message = "Email təsdiqləndi və qeydiyyat tamamlandı.",
            MaskedEmail = MaskEmail(user.Email),
            ExpiresInSeconds = 0,
            ResendInSeconds = 0,
            Expired = false,
            CanResend = false,
            User = ToDto(user)
        };
    }

    public async Task<EmailRegistrationResponse>
        ResendEmailRegistrationCodeAsync(
            ResendEmailRegistrationCodeRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request.VerificationId == Guid.Empty)
        {
            return EmailRegistrationFailed(
                "Verification ID düzgün deyil.",
                EmailRegistrationErrorCodes.Validation);
        }

        var pending = await _dbContext.PendingEmailRegistrations
            .FirstOrDefaultAsync(
                item => item.Id == request.VerificationId,
                cancellationToken);

        if (pending is null)
        {
            return EmailRegistrationFailed(
                "Qeydiyyat sorğusu tapılmadı. Yenidən qeydiyyata başlayın.",
                EmailRegistrationErrorCodes.NotFound);
        }

        var now = UtcNow();

        if (await EmailAlreadyRegisteredAsync(
                pending.Email,
                cancellationToken))
        {
            _dbContext.PendingEmailRegistrations.Remove(pending);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return EmailRegistrationFailed(
                "Bu email ilə artıq qeydiyyatdan keçilib.",
                EmailRegistrationErrorCodes.DuplicateEmail);
        }

        if (pending.ResendAvailableAtUtc > now)
        {
            return EmailRegistrationFailed(
                "Yeni kod yalnız 1 dəqiqə tamamlandıqdan sonra göndərilə bilər.",
                EmailRegistrationErrorCodes.ResendTooEarly,
                pending,
                now);
        }

        var verificationCode = CreateVerificationCode();
        ApplyNewVerificationCode(
            pending,
            verificationCode,
            now);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var emailSent = await TrySendVerificationCodeAsync(
            pending,
            verificationCode,
            cancellationToken);

        if (!emailSent)
        {
            await MakeImmediateRetryAvailableAsync(
                pending,
                cancellationToken);

            return EmailRegistrationFailed(
                "Yeni kod email-ə göndərilmədi. Outlook/Microsoft Graph konfiqurasiyasını yoxlayın.",
                EmailRegistrationErrorCodes.EmailDeliveryFailed,
                pending,
                UtcNow());
        }

        return BuildEmailRegistrationStatus(
            pending,
            now,
            true,
            "Yeni təsdiq kodu email ünvanınıza göndərildi.");
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Login)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            return Failed(
                "Login və password boş ola bilməz.");
        }

        var login = request.Login.Trim();
        var normalizedLogin = login.ToLowerInvariant();

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Email.ToLower() == normalizedLogin ||
                x.UserName.ToLower() == normalizedLogin ||
                x.PhoneNumber == login,
                cancellationToken);

        if (user is null)
        {
            return Failed(
                "Email/username/telefon və ya password yanlışdır.");
        }

        var passwordOk = PasswordHasher.VerifyPassword(
            request.Password,
            user.PasswordHash);

        if (!passwordOk)
        {
            return Failed(
                "Email/username/telefon və ya password yanlışdır.");
        }

        return new AuthResponse
        {
            Success = true,
            Message = "Sign in uğurludur.",
            User = ToDto(user)
        };
    }

    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Email.ToLower() == email,
                cancellationToken);

        // Security: email tapılmasa belə generic cavab veririk.
        if (user is null)
        {
            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "Əgər bu email sistemdə varsa, reset kod göndəriləcək."
            };
        }

        var resetCode = RandomNumberGenerator
            .GetInt32(100000, 1000000)
            .ToString();
        var now = UtcNow();

        user.PasswordResetCodeHash =
            PasswordHasher.HashPassword(resetCode);
        user.PasswordResetCodeExpiresAt =
            now.AddMinutes(15);
        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResponse
        {
            Success = true,
            Message = "Reset kod yaradıldı. Email/SMS provider qoşulanda kod istifadəçiyə göndəriləcək.",
            DevelopmentResetCode =
                _environment.IsDevelopment()
                    ? resetCode
                    : null
        };
    }

    public async Task<AuthResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.ResetCode)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Failed(
                "Email, reset kod və yeni password mütləqdir.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Email.ToLower() == email,
                cancellationToken);

        if (user is null
            || string.IsNullOrWhiteSpace(
                user.PasswordResetCodeHash)
            || user.PasswordResetCodeExpiresAt is null)
        {
            return Failed(
                "Reset kod yanlışdır və ya vaxtı bitib.");
        }

        var now = UtcNow();

        if (user.PasswordResetCodeExpiresAt < now)
        {
            return Failed(
                "Reset kodun vaxtı bitib.");
        }

        var codeOk = PasswordHasher.VerifyPassword(
            request.ResetCode.Trim(),
            user.PasswordResetCodeHash);

        if (!codeOk)
        {
            return Failed(
                "Reset kod yanlışdır və ya vaxtı bitib.");
        }

        user.PasswordHash =
            PasswordHasher.HashPassword(
                request.NewPassword);
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAt = null;
        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Password yeniləndi. İndi sign in edə bilərsən.",
            User = ToDto(user)
        };
    }

    private async Task<bool> TrySendVerificationCodeAsync(
        PendingEmailRegistration pending,
        string verificationCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await _registrationEmailSender.SendVerificationCodeAsync(
                pending.Email,
                verificationCode,
                VerificationLifetime,
                cancellationToken);

            return true;
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                exception,
                "Registration verification mail sorğusunun vaxtı bitdi.");

            return false;
        }
        catch (Exception exception)
            when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Registration verification kodu {Email} ünvanına göndərilmədi.",
                pending.Email);

            return false;
        }
    }

    private async Task MakeImmediateRetryAvailableAsync(
        PendingEmailRegistration pending,
        CancellationToken cancellationToken)
    {
        var now = UtcNow();
        pending.VerificationCodeExpiresAtUtc = now;
        pending.ResendAvailableAtUtc = now;
        pending.UpdatedAtUtc = now;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(
                exception,
                "Email delivery failure-dan sonra registration retry vaxtı yenilənmədi.");
        }
    }

    private Task<bool> EmailAlreadyRegisteredAsync(
        string email,
        CancellationToken cancellationToken)
    {
        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                item => item.Email.ToLower() == email,
                cancellationToken);
    }

    private static void NormalizeEmailRegistrationRequest(
        StartEmailRegistrationRequest request)
    {
        request.ProfileName =
            request.ProfileName?.Trim() ?? string.Empty;
        request.Email =
            request.Email?.Trim().ToLowerInvariant()
            ?? string.Empty;
        request.AccountType =
            request.AccountType?.Trim().ToLowerInvariant()
            ?? string.Empty;
        request.CompanyName =
            request.CompanyName?.Trim();
        request.CompanyType =
            request.CompanyType?.Trim();
        request.Industry =
            request.Industry?.Trim();
        request.InvitationToken =
            request.InvitationToken?.Trim();
    }

    private static string ValidateEmailRegistration(
        StartEmailRegistrationRequest request,
        bool isTeamInvitation)
    {

        if (string.IsNullOrWhiteSpace(request.ProfileName))
            return "Profil və ya şirkət adı boş ola bilməz.";

        if (string.IsNullOrWhiteSpace(request.Email))
            return "Email boş ola bilməz.";

        if (string.IsNullOrEmpty(request.Password)
            || request.Password.Length < 8)
            return "Password ən azı 8 simvol olmalıdır.";

        if (!request.AcceptedTerms)
            return "Terms və privacy policy qəbul edilməlidir.";

        if (request.AccountType is not ("candidate" or "employer"))
            return "Account type candidate və ya employer olmalıdır.";

        if (request.AccountType == "employer")
        {
            if (!string.IsNullOrWhiteSpace(request.CompanyType))
            {
                request.CompanyType = request.CompanyType switch
                {
                    var value when value.Equals(
                        "Startup",
                        StringComparison.OrdinalIgnoreCase)
                        => "Startup",
                    var value when value.Equals(
                        "SME",
                        StringComparison.OrdinalIgnoreCase)
                        => "SME",
                    var value when value.Equals(
                        "Corporate",
                        StringComparison.OrdinalIgnoreCase)
                        => "Corporate",
                    _ => string.Empty
                };
            }

            if (!isTeamInvitation
                && string.IsNullOrWhiteSpace(request.CompanyType))
                return "Company type seçilməlidir.";

            if (!isTeamInvitation
                && string.IsNullOrWhiteSpace(request.Industry))
                return "Industry boş ola bilməz.";

            request.CompanyName =
                string.IsNullOrWhiteSpace(request.CompanyName)
                    ? request.ProfileName
                    : request.CompanyName;
        }
        else
        {
            request.CompanyName = null;
            request.CompanyType = null;
            request.Industry = null;
            request.InvitationToken = null;
        }

        return string.Empty;
    }

    private static void ApplyRegistrationData(
        PendingEmailRegistration pending,
        StartEmailRegistrationRequest request,
        Guid? teamInvitationId,
        DateTime now,
        bool updatePassword)
    {
        pending.Email = request.Email;
        pending.ProfileName = request.ProfileName;
        pending.AccountType = request.AccountType;
        pending.CompanyName = request.CompanyName;
        pending.CompanyType = request.CompanyType;
        pending.Industry = request.Industry;
        pending.TeamInvitationId = teamInvitationId;
        pending.UpdatedAtUtc = now;

        if (updatePassword)
        {
            pending.PasswordHash =
                PasswordHasher.HashPassword(
                    request.Password);
        }
    }

    private static void ApplyNewVerificationCode(
        PendingEmailRegistration pending,
        string verificationCode,
        DateTime now)
    {
        pending.VerificationCodeHash =
            PasswordHasher.HashPassword(
                BuildCodeSecret(
                    pending.Id,
                    verificationCode));
        pending.VerificationCodeExpiresAtUtc =
            now.Add(VerificationLifetime);
        pending.ResendAvailableAtUtc =
            now.Add(VerificationLifetime);
        pending.LastSentAtUtc = now;
        pending.FailedAttemptCount = 0;
        pending.UpdatedAtUtc = now;
    }

    private static User BuildVerifiedUser(
        PendingEmailRegistration pending,
        DateTime now,
        bool isCompanyTeamMember)
    {
        var (name, surname) = SplitProfileName(
            pending.ProfileName,
            pending.AccountType,
            isCompanyTeamMember);

        return new User
        {
            UserName = CreateUserName(
                pending.Email,
                pending.Id),
            Name = name,
            Surname = surname,
            PhoneNumber = null,
            Email = pending.Email,
            PasswordHash = pending.PasswordHash,
            AccountType = pending.AccountType,
            CompanyName =
                pending.AccountType == "employer"
                    ? pending.CompanyName ?? pending.ProfileName
                    : null,
            CompanyType = pending.CompanyType,
            Industry = pending.Industry,
            EmailVerifiedAtUtc = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    private static (string Name, string Surname)
        SplitProfileName(
            string profileName,
            string accountType,
            bool isCompanyTeamMember)
    {
        if (accountType == "employer"
            && !isCompanyTeamMember)
            return (profileName, string.Empty);

        var parts = profileName.Split(
            ' ',
            2,
            StringSplitOptions.RemoveEmptyEntries
            | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => ("User", string.Empty),
            1 => (parts[0], string.Empty),
            _ => (parts[0], parts[1])
        };
    }

    private static string CreateUserName(
        string email,
        Guid verificationId)
    {
        var localPart =
            email.Split('@', 2)[0];
        var safeLocalPart = new string(
            localPart
                .Where(character =>
                    char.IsLetterOrDigit(character)
                    || character is '.' or '_' or '-')
                .Take(60)
                .ToArray())
            .Trim('.', '_', '-');

        if (string.IsNullOrWhiteSpace(safeLocalPart))
            safeLocalPart = "user";

        return $"{safeLocalPart}_{verificationId:N}"[..Math.Min(
            safeLocalPart.Length + 9,
            80)];
    }

    private static string GetCompanyName(User owner)
    {
        if (!string.IsNullOrWhiteSpace(owner.CompanyName))
            return owner.CompanyName;

        if (!string.IsNullOrWhiteSpace(owner.Name))
            return owner.Name;

        return owner.Email;
    }

    private static string CreateVerificationCode()
    {
        return RandomNumberGenerator
            .GetInt32(0, 1_000_000)
            .ToString("D6");
    }

    private static string BuildCodeSecret(
        Guid verificationId,
        string verificationCode)
    {
        return $"{verificationId:N}:{verificationCode}";
    }

    private static EmailRegistrationResponse
        BuildEmailRegistrationStatus(
            PendingEmailRegistration pending,
            DateTime now,
            bool success,
            string message)
    {
        var expiresInSeconds = SecondsUntil(
            pending.VerificationCodeExpiresAtUtc,
            now);
        var resendInSeconds = SecondsUntil(
            pending.ResendAvailableAtUtc,
            now);

        return new EmailRegistrationResponse
        {
            Success = success,
            Message = message,
            VerificationId = pending.Id,
            MaskedEmail = MaskEmail(pending.Email),
            ExpiresAtUtc =
                pending.VerificationCodeExpiresAtUtc,
            ResendAvailableAtUtc =
                pending.ResendAvailableAtUtc,
            ExpiresInSeconds = expiresInSeconds,
            ResendInSeconds = resendInSeconds,
            Expired = expiresInSeconds == 0,
            CanResend = resendInSeconds == 0
        };
    }

    private static EmailRegistrationResponse
        EmailRegistrationFailed(
            string message,
            string errorCode,
            PendingEmailRegistration? pending = null,
            DateTime? now = null)
    {
        if (pending is null)
        {
            return new EmailRegistrationResponse
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }

        var response = BuildEmailRegistrationStatus(
            pending,
            now ?? DateTime.UtcNow,
            false,
            message);
        response.ErrorCode = errorCode;

        return response;
    }

    private static int SecondsUntil(
        DateTime targetUtc,
        DateTime nowUtc)
    {
        return Math.Max(
            0,
            (int)Math.Ceiling(
                (targetUtc - nowUtc).TotalSeconds));
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@', 2);

        if (parts.Length != 2)
            return email;

        var localPart = parts[0];
        var visiblePrefix = localPart.Length switch
        {
            0 => string.Empty,
            1 => localPart,
            _ => localPart[..Math.Min(2, localPart.Length)]
        };

        return $"{visiblePrefix}***@{parts[1]}";
    }

    private static void NormalizeRegisterRequest(
        RegisterRequest request)
    {
        request.UserName = request.UserName.Trim();
        request.Name = request.Name.Trim();
        request.Surname = request.Surname.Trim();
        request.PhoneNumber = request.PhoneNumber.Trim();
        request.Email =
            request.Email.Trim().ToLowerInvariant();

        if (request.Password.Length < 8)
        {
            throw new ArgumentException(
                "Password ən azı 8 simvol olmalıdır.");
        }
    }

    private static AuthUserDto ToDto(User user)
    {
        return new AuthUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Email = user.Email,
            AccountType = user.AccountType,
            CompanyName = user.CompanyName,
            CompanyType = user.CompanyType,
            Industry = user.Industry
        };
    }

    private DateTime UtcNow()
    {
        return _timeProvider
            .GetUtcNow()
            .UtcDateTime;
    }

    private static AuthResponse Failed(string message)
    {
        return new AuthResponse
        {
            Success = false,
            Message = message
        };
    }
}
