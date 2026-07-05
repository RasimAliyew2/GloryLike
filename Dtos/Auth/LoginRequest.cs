using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.Auth;

public class LoginRequest
{
    [Required]
    [MaxLength(150)]
    public string Login { get; set; } = string.Empty; // email, phone number, or username

    [Required]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
