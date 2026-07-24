namespace GloryLikeBackend.Models;

public sealed class PendingEmailRegistration
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string ProfileName { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;

    public string? CompanyType { get; set; }

    public string? Industry { get; set; }

    public string VerificationCodeHash { get; set; } = string.Empty;

    public DateTime VerificationCodeExpiresAtUtc { get; set; }

    public DateTime ResendAvailableAtUtc { get; set; }

    public DateTime LastSentAtUtc { get; set; }

    public int FailedAttemptCount { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
