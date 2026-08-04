namespace GloryLikeBackend.Services.Interfaces;

public interface IRegistrationEmailSender
{
    Task SendVerificationCodeAsync(
        string recipientEmail,
        string verificationCode,
        TimeSpan validFor,
        CancellationToken cancellationToken = default);

    Task SendTeamInvitationAsync(
        string recipientEmail,
        string companyName,
        string role,
        string invitationUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}
