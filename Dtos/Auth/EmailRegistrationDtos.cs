using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.Auth;

public sealed class StartEmailRegistrationRequest
{
    [Required]
    [StringLength(150)]
    public string ProfileName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(candidate|employer)$")]
    public string AccountType { get; set; } = string.Empty;

    [StringLength(30)]
    public string? CompanyType { get; set; }

    [StringLength(120)]
    public string? Industry { get; set; }

    [Range(
        typeof(bool),
        "true",
        "true",
        ErrorMessage = "Terms və privacy policy qəbul edilməlidir.")]
    public bool AcceptedTerms { get; set; }
}

public sealed class VerifyEmailRegistrationRequest
{
    public Guid VerificationId { get; set; }

    [Required]
    [RegularExpression(
        "^\\d{6}$",
        ErrorMessage = "Təsdiq kodu 6 rəqəmdən ibarət olmalıdır.")]
    public string Code { get; set; } = string.Empty;
}

public sealed class ResendEmailRegistrationCodeRequest
{
    public Guid VerificationId { get; set; }
}

public sealed class EmailRegistrationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public Guid? VerificationId { get; set; }

    public string MaskedEmail { get; set; } = string.Empty;

    public DateTime? ExpiresAtUtc { get; set; }

    public DateTime? ResendAvailableAtUtc { get; set; }

    public int ExpiresInSeconds { get; set; }

    public int ResendInSeconds { get; set; }

    public bool Expired { get; set; }

    public bool CanResend { get; set; }

    public AuthUserDto? User { get; set; }
}

public static class EmailRegistrationErrorCodes
{
    public const string Validation = "validation";
    public const string DuplicateEmail = "duplicate_email";
    public const string NotFound = "not_found";
    public const string Expired = "expired";
    public const string InvalidCode = "invalid_code";
    public const string TooManyAttempts = "too_many_attempts";
    public const string ResendTooEarly = "resend_too_early";
    public const string EmailDeliveryFailed = "email_delivery_failed";
    public const string Conflict = "conflict";
}
