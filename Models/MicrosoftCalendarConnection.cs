namespace GloryLikeBackend.Models;

public sealed class MicrosoftCalendarConnection
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string MicrosoftUserId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ProtectedAccessToken { get; set; } = string.Empty;

    public string ProtectedRefreshToken { get; set; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; set; }

    public string GrantedScopes { get; set; } = string.Empty;

    public DateTime ConnectedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public User User { get; set; } = null!;
}
