using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.Auth;

public class RegisterRequest
{
    [Required]
    [MaxLength(80)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Surname { get; set; } = string.Empty;

    [Required]
    [Phone]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
