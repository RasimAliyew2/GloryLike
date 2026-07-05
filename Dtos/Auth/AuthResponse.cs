namespace GloryLikeBackend.Dtos.Auth;

public class AuthResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public AuthUserDto? User { get; set; }
}
