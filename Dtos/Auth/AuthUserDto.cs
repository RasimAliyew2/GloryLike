namespace GloryLikeBackend.Dtos.Auth;

public class AuthUserDto
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public string? CompanyType { get; set; }

    public string? Industry { get; set; }
}
