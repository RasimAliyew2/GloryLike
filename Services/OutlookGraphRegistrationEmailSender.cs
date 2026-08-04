using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GloryLikeBackend.Options;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace GloryLikeBackend.Services;

public sealed class OutlookGraphRegistrationEmailSender
    : IRegistrationEmailSender
{
    private const string GraphScope =
        "https://graph.microsoft.com/.default";

    private readonly HttpClient _httpClient;
    private readonly OutlookMailOptions _options;
    private readonly ILogger<OutlookGraphRegistrationEmailSender> _logger;

    public OutlookGraphRegistrationEmailSender(
        HttpClient httpClient,
        IOptions<OutlookMailOptions> options,
        ILogger<OutlookGraphRegistrationEmailSender> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendVerificationCodeAsync(
        string recipientEmail,
        string verificationCode,
        TimeSpan validFor,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var accessToken =
            await GetAccessTokenAsync(
                cancellationToken);
        var senderEmail =
            Uri.EscapeDataString(
                _options.SenderEmail.Trim());

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://graph.microsoft.com/v1.0/users/{senderEmail}/sendMail");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
        request.Content = JsonContent.Create(
            new
            {
                message = new
                {
                    subject =
                        "SkillMatch email verification code",
                    body = new
                    {
                        contentType = "HTML",
                        content = BuildHtmlBody(
                            verificationCode,
                            validFor)
                    },
                    toRecipients = new[]
                    {
                        new
                        {
                            emailAddress = new
                            {
                                address = recipientEmail
                            }
                        }
                    }
                },
                saveToSentItems = true
            });

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        _logger.LogError(
            "Microsoft Graph verification email request failed with status {StatusCode}.",
            (int)response.StatusCode);

        throw new HttpRequestException(
            "Microsoft Graph verification email göndərmədi.",
            inner: null,
            response.StatusCode);
    }

    public async Task SendTeamInvitationAsync(
        string recipientEmail,
        string companyName,
        string role,
        string invitationUrl,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        var accessToken =
            await GetAccessTokenAsync(
                cancellationToken);
        var senderEmail =
            Uri.EscapeDataString(
                _options.SenderEmail.Trim());

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://graph.microsoft.com/v1.0/users/{senderEmail}/sendMail");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
        request.Content = JsonContent.Create(
            new
            {
                message = new
                {
                    subject =
                        $"{companyName} invited you to SkillMatch",
                    body = new
                    {
                        contentType = "HTML",
                        content = BuildTeamInvitationHtml(
                            companyName,
                            role,
                            invitationUrl,
                            expiresAtUtc)
                    },
                    toRecipients = new[]
                    {
                        new
                        {
                            emailAddress = new
                            {
                                address = recipientEmail
                            }
                        }
                    }
                },
                saveToSentItems = true
            });

        using var response =
            await _httpClient.SendAsync(
                request,
                cancellationToken);

        if (response.IsSuccessStatusCode)
            return;

        _logger.LogError(
            "Microsoft Graph team invitation request failed with status {StatusCode}.",
            (int)response.StatusCode);

        throw new HttpRequestException(
            "Microsoft Graph team invitation email göndərmədi.",
            inner: null,
            response.StatusCode);
    }

    private async Task<string> GetAccessTokenAsync(
        CancellationToken cancellationToken)
    {
        var tenantId =
            Uri.EscapeDataString(
                _options.TenantId.Trim());
        var tokenEndpoint =
            $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token";

        using var tokenRequest =
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["client_id"] =
                        _options.ClientId.Trim(),
                    ["client_secret"] =
                        _options.ClientSecret,
                    ["scope"] = GraphScope,
                    ["grant_type"] =
                        "client_credentials"
                });

        using var response =
            await _httpClient.PostAsync(
                tokenEndpoint,
                tokenRequest,
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Microsoft identity token request failed with status {StatusCode}.",
                (int)response.StatusCode);

            throw new HttpRequestException(
                "Microsoft identity access token vermədi.",
                inner: null,
                response.StatusCode);
        }

        var tokenResponse =
            await response.Content
                .ReadFromJsonAsync<AccessTokenResponse>(
                    cancellationToken:
                        cancellationToken);

        if (string.IsNullOrWhiteSpace(
                tokenResponse?.AccessToken))
        {
            throw new InvalidOperationException(
                "Microsoft identity cavabında access token yoxdur.");
        }

        return tokenResponse.AccessToken;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.TenantId)
            || string.IsNullOrWhiteSpace(_options.ClientId)
            || string.IsNullOrWhiteSpace(_options.ClientSecret)
            || string.IsNullOrWhiteSpace(_options.SenderEmail))
        {
            throw new InvalidOperationException(
                "Outlook/Microsoft Graph konfiqurasiyası tamamlanmayıb. "
                + "TenantId, ClientId, ClientSecret və SenderEmail "
                + "User Secrets və ya environment variables ilə verilməlidir.");
        }
    }

    private static string BuildHtmlBody(
        string verificationCode,
        TimeSpan validFor)
    {
        var validSeconds = Math.Max(
            1,
            (int)Math.Ceiling(
                validFor.TotalSeconds));

        return $$"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:24px;background:#f6f8fb;font-family:Arial,sans-serif;color:#101828;">
              <div style="max-width:520px;margin:0 auto;padding:32px;background:#ffffff;border:1px solid #e4e7ec;border-radius:16px;">
                <div style="font-size:20px;font-weight:700;color:#4f83ff;">SkillMatch</div>
                <h1 style="margin:22px 0 10px;font-size:24px;">Verify your email</h1>
                <p style="margin:0 0 22px;color:#667085;line-height:1.55;">
                  Enter this code on the registration page:
                </p>
                <div style="padding:18px;text-align:center;background:#f1f5ff;border-radius:12px;font-size:34px;font-weight:800;letter-spacing:10px;color:#356fe4;">
                  {{verificationCode}}
                </div>
                <p style="margin:22px 0 0;color:#667085;line-height:1.55;">
                  The code is valid for {{validSeconds}} seconds. If you did not request this code, you can ignore this email.
                </p>
              </div>
            </body>
            </html>
            """;
    }

    private static string BuildTeamInvitationHtml(
        string companyName,
        string role,
        string invitationUrl,
        DateTime expiresAtUtc)
    {
        var safeCompany =
            WebUtility.HtmlEncode(companyName);
        var safeRole =
            WebUtility.HtmlEncode(role);
        var safeUrl =
            WebUtility.HtmlEncode(invitationUrl);
        var safeExpiry =
            WebUtility.HtmlEncode(
                expiresAtUtc.ToString(
                    "dd MMM yyyy HH:mm 'UTC'"));

        return $$"""
            <!doctype html>
            <html lang="en">
            <body style="margin:0;padding:24px;background:#f6f8fb;font-family:Arial,sans-serif;color:#101828;">
              <div style="max-width:560px;margin:0 auto;padding:32px;background:#ffffff;border:1px solid #e4e7ec;border-radius:16px;">
                <div style="font-size:20px;font-weight:700;color:#5548e8;">SkillMatch</div>
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
    }

    private sealed class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } =
            string.Empty;
    }
}
