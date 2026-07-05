namespace GloryLikeBackend.Dtos.Auth;

public class ForgotPasswordResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Development-only field. Production-da email/SMS provider qoşulandan sonra null qalmalıdır.
    /// </summary>
    public string? DevelopmentResetCode { get; set; }
}
