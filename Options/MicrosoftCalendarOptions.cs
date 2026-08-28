namespace GloryLikeBackend.Options;

public sealed class MicrosoftCalendarOptions
{
    public const string SectionName = "MicrosoftCalendar";

    public string Tenant { get; set; } = "common";

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string BackendSharedSecret { get; set; } = string.Empty;

    public string[] Scopes { get; set; } =
    [
        "openid",
        "profile",
        "email",
        "offline_access",
        "User.Read",
        "Calendars.ReadWrite"
    ];
}
