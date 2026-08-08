namespace GloryLikeBackend.Options;

public sealed class SocialAuthOptions
{
    public const string SectionName = "SocialAuth";

    public string BackendSharedSecret { get; set; } = string.Empty;
}
