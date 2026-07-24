namespace GloryLikeBackend.Options;

public sealed class OutlookMailOptions
{
    public const string SectionName = "OutlookMail";

    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;
}
