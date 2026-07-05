using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Auth;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Hash;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace GloryLikeBackend.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public AuthService(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
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
            if (duplicate.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
                return Failed("Bu email ilə artıq qeydiyyatdan keçilib.");

            if (duplicate.PhoneNumber == request.PhoneNumber)
                return Failed("Bu telefon nömrəsi ilə artıq qeydiyyatdan keçilib.");

            return Failed("Bu username ilə artıq qeydiyyatdan keçilib.");
        }

        var user = new User
        {
            UserName = request.UserName,
            Name = request.Name,
            Surname = request.Surname,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password))
            return Failed("Login və password boş ola bilməz.");

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
            return Failed("Email/username/telefon və ya password yanlışdır.");

        var passwordOk = PasswordHasher.VerifyPassword(request.Password, user.PasswordHash);

        if (!passwordOk)
            return Failed("Email/username/telefon və ya password yanlışdır.");

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
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        // Security: email tapılmasa belə generic cavab veririk.
        if (user is null)
        {
            return new ForgotPasswordResponse
            {
                Success = true,
                Message = "Əgər bu email sistemdə varsa, reset kod göndəriləcək."
            };
        }

        var resetCode = RandomNumberGenerator.GetInt32(100000, 999999).ToString();

        user.PasswordResetCodeHash = PasswordHasher.HashPassword(resetCode);
        user.PasswordResetCodeExpiresAt = DateTime.UtcNow.AddMinutes(15);
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ForgotPasswordResponse
        {
            Success = true,
            Message = "Reset kod yaradıldı. Email/SMS provider qoşulanda kod istifadəçiyə göndəriləcək.",
            DevelopmentResetCode = _environment.IsDevelopment() ? resetCode : null
        };
    }

    public async Task<AuthResponse> ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.ResetCode) ||
            string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return Failed("Email, reset kod və yeni password mütləqdir.");
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email, cancellationToken);

        if (user is null ||
            string.IsNullOrWhiteSpace(user.PasswordResetCodeHash) ||
            user.PasswordResetCodeExpiresAt is null)
        {
            return Failed("Reset kod yanlışdır və ya vaxtı bitib.");
        }

        if (user.PasswordResetCodeExpiresAt < DateTime.UtcNow)
            return Failed("Reset kodun vaxtı bitib.");

        var codeOk = PasswordHasher.VerifyPassword(
            request.ResetCode.Trim(),
            user.PasswordResetCodeHash);

        if (!codeOk)
            return Failed("Reset kod yanlışdır və ya vaxtı bitib.");

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            Success = true,
            Message = "Password yeniləndi. İndi sign in edə bilərsən.",
            User = ToDto(user)
        };
    }

    private static void NormalizeRegisterRequest(RegisterRequest request)
    {
        request.UserName = request.UserName.Trim();
        request.Name = request.Name.Trim();
        request.Surname = request.Surname.Trim();
        request.PhoneNumber = request.PhoneNumber.Trim();
        request.Email = request.Email.Trim().ToLowerInvariant();

        if (request.Password.Length < 8)
            throw new ArgumentException("Password ən azı 8 simvol olmalıdır.");
    }

    private static AuthUserDto ToDto(User user)
    {
        return new AuthUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Name = user.Name,
            Surname = user.Surname,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email
        };
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
