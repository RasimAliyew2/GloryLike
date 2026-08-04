namespace GloryLikeBackend.Options;

public sealed class TeamInvitationOptions
{
    public const string SectionName = "TeamInvitations";

    public string WebAppBaseUrl { get; set; } =
        "https://localhost:7245";

    public int LifetimeDays { get; set; } = 7;
}
