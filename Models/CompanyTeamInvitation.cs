namespace GloryLikeBackend.Models;

public sealed class CompanyTeamInvitation
{
    public Guid Id { get; set; }

    public int OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    public int? AcceptedUserId { get; set; }

    public User? AcceptedUser { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = CompanyTeamInvitationStatuses.Invited;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime SentAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }
}

public static class CompanyTeamInvitationStatuses
{
    public const string Invited = "Invited";

    public const string Active = "Active";
}
