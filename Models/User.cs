namespace GloryLikeBackend.Models;

public class User
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string? FatherName { get; set; }

    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string AccountType { get; set; } = "candidate";

    public string? CompanyName { get; set; }

    public string? CompanyType { get; set; }

    public string? Industry { get; set; }

    public DateTime? EmailVerifiedAtUtc { get; set; }

    public int Age { get; set; }

    public DateTime? BirthDate { get; set; }

    public string? About { get; set; }

    public string? ProfileImageDataUrl { get; set; }

    public string? PasswordResetCodeHash { get; set; }

    public DateTime? PasswordResetCodeExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
