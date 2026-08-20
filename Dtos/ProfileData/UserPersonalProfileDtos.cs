using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.ProfileData;

public sealed class UpdateUserPersonalProfileRequest
{
    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(80, MinimumLength = 1)]
    public string LastName { get; set; } = string.Empty;

    public DateTime? BirthDate { get; set; }

    [StringLength(1000)]
    public string? About { get; set; }

    public string? ProfileImageDataUrl { get; set; }
}

public sealed class UserPersonalProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string About { get; set; } = string.Empty;
    public string ProfileImageDataUrl { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
}
