namespace GloryLikeBackend.Services.Interfaces;

public interface IRegistrationEmailSender
{
    Task SendVerificationCodeAsync(
        string recipientEmail,
        string verificationCode,
        TimeSpan validFor,
        CancellationToken cancellationToken = default);
}
