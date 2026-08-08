using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.Auth;

public sealed class SocialLoginRequest
{
    [Required]
    [MaxLength(20)]
    public string Provider { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string ProviderSubject { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(80)]
    public string FirstName { get; set; } = string.Empty;

    [MaxLength(80)]
    public string LastName { get; set; } = string.Empty;
}
