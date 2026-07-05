using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.Auth;

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}
