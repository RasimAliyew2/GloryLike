using System.Net;
using System.Net.Mail;
using GloryLikeBackend.Options;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace GloryLikeBackend.Services;

public sealed class SmtpRegistrationEmailSender
    : IRegistrationEmailSender
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpRegistrationEmailSender> _logger;

    public SmtpRegistrationEmailSender(
        IOptions<SmtpOptions> options,
        ILogger<SmtpRegistrationEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task SendVerificationCodeAsync(
        string recipientEmail,
        string verificationCode,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        var validSeconds = Math.Max(
            1,
            (int)Math.Ceiling(validFor.TotalSeconds));
        var safeCode = WebUtility.HtmlEncode(
            verificationCode);

        var body = $$"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:24px;background:#f6f8fb;font-family:Arial,sans-serif;color:#101828;">
              <div style="max-width:520px;margin:0 auto;padding:32px;background:#ffffff;border:1px solid #e4e7ec;border-radius:16px;">
                <div style="font-size:20px;font-weight:700;color:#4f83ff;">GloryLike</div>
                <h1 style="margin:22px 0 10px;font-size:24px;">Verify your email</h1>
                <p style="margin:0 0 22px;color:#667085;line-height:1.55;">
                  Enter this code on the registration page:
                </p>
                <div style="padding:18px;text-align:center;background:#f1f5ff;border-radius:12px;font-size:34px;font-weight:800;letter-spacing:10px;color:#356fe4;">
                  {{safeCode}}
                </div>
                <p style="margin:22px 0 0;color:#667085;line-height:1.55;">
                  The code is valid for {{validSeconds}} seconds. If you did not request this code, you can ignore this email.
                </p>
              </div>
            </body>
            </html>
            """;

        return SendAsync(
            recipientEmail,
            "GloryLike email verification code",
            body,
            cancellationToken);
    }

    public Task SendTeamInvitationAsync(
        string recipientEmail,
        string companyName,
        string role,
        string invitationUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var safeCompany = WebUtility.HtmlEncode(
            companyName);
        var safeRole = WebUtility.HtmlEncode(role);
        var safeUrl = WebUtility.HtmlEncode(
            invitationUrl);
        var safeExpiry = WebUtility.HtmlEncode(
            expiresAtUtc.ToString(
                "dd MMM yyyy HH:mm 'UTC'"));

        var body = $$"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:24px;background:#f6f8fb;font-family:Arial,sans-serif;color:#101828;">
              <div style="max-width:560px;margin:0 auto;padding:32px;background:#ffffff;border:1px solid #e4e7ec;border-radius:16px;">
                <div style="font-size:20px;font-weight:700;color:#5548e8;">GloryLike</div>
                <h1 style="margin:22px 0 10px;font-size:24px;">Join {{safeCompany}}</h1>
                <p style="margin:0 0 20px;color:#667085;line-height:1.55;">
                  You were invited to the company team as <strong>{{safeRole}}</strong>.
                </p>
                <a href="{{safeUrl}}" style="display:inline-block;padding:13px 22px;background:#5548e8;color:#ffffff;text-decoration:none;border-radius:10px;font-weight:700;">
                  Accept invitation
                </a>
                <p style="margin:22px 0 0;color:#667085;line-height:1.55;">
                  This invitation expires on {{safeExpiry}}. If you were not expecting it, you can ignore this email.
                </p>
              </div>
            </body>
            </html>
            """;

        return SendAsync(
            recipientEmail,
            $"{companyName} invited you to GloryLike",
            body,
            cancellationToken);
    }

    private async Task SendAsync(
        string recipientEmail,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        using var message = new MailMessage
        {
            From = new MailAddress(
                _options.FromEmail.Trim(),
                _options.FromName.Trim()),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(
            new MailAddress(recipientEmail));

        using var client = new SmtpClient(
            _options.Host.Trim(),
            _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            DeliveryMethod =
                SmtpDeliveryMethod.Network,
            Timeout = 30_000
        };

        if (!string.IsNullOrWhiteSpace(
                _options.Username))
        {
            client.Credentials =
                new NetworkCredential(
                    _options.Username,
                    _options.Password);
        }

        try
        {
            await client.SendMailAsync(
                message,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "SMTP email could not be sent.");
            throw;
        }
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.Host)
            || string.IsNullOrWhiteSpace(
                _options.FromEmail)
            || _options.Port is < 1 or > 65535)
        {
            throw new InvalidOperationException(
                "SMTP konfiqurasiyası tamamlanmayıb. "
                + "Smtp:Host, Smtp:Port və "
                + "Smtp:FromEmail dəyərləri verilməlidir.");
        }
    }
}
