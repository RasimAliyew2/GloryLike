using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.MicrosoftCalendar;
using GloryLikeBackend.Models;
using GloryLikeBackend.Options;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GloryLikeBackend.Services;

public sealed class MicrosoftCalendarService : IMicrosoftCalendarService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly HttpClient _httpClient;
    private readonly MicrosoftCalendarOptions _options;
    private readonly IDataProtector _tokenProtector;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MicrosoftCalendarService> _logger;

    public MicrosoftCalendarService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService,
        HttpClient httpClient,
        IOptions<MicrosoftCalendarOptions> options,
        IDataProtectionProvider dataProtectionProvider,
        IWebHostEnvironment environment,
        ILogger<MicrosoftCalendarService> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _httpClient = httpClient;
        _options = options.Value;
        _tokenProtector = dataProtectionProvider.CreateProtector(
            "BothFind.MicrosoftCalendar.Tokens.v1");
        _environment = environment;
        _logger = logger;
    }

    public async Task<MicrosoftCalendarAuthorizationUrlResponse>
        CreateAuthorizationUrlAsync(
            MicrosoftCalendarAuthorizationUrlRequest request,
            CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
            return AuthorizationFailure("Microsoft Calendar konfiqurasiyası tamamlanmayıb.");

        if (await _companyAccessService.ResolveAsync(
                request.EmployerUserId,
                cancellationToken) is null)
        {
            return AuthorizationFailure("Employer hesabı tapılmadı və ya aktiv deyil.");
        }

        if (!IsAllowedRedirectUri(request.RedirectUri)
            || string.IsNullOrWhiteSpace(request.State)
            || request.State.Length > 300
            || string.IsNullOrWhiteSpace(request.CodeChallenge)
            || request.CodeChallenge.Length > 128)
        {
            return AuthorizationFailure("Microsoft bağlantı sorğusu düzgün deyil.");
        }

        var tenant = NormalizeTenant();
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId.Trim(),
            ["response_type"] = "code",
            ["redirect_uri"] = request.RedirectUri,
            ["response_mode"] = "query",
            ["scope"] = string.Join(' ', GetScopes()),
            ["state"] = request.State,
            ["code_challenge"] = request.CodeChallenge,
            ["code_challenge_method"] = "S256",
            ["prompt"] = "select_account"
        };
        var authorizationUrl =
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/oauth2/v2.0/authorize"
            + "?"
            + string.Join("&", query.Select(item =>
                $"{Uri.EscapeDataString(item.Key)}={Uri.EscapeDataString(item.Value)}"));

        return new MicrosoftCalendarAuthorizationUrlResponse
        {
            Success = true,
            Message = "Microsoft authorization URL yaradıldı.",
            AuthorizationUrl = authorizationUrl
        };
    }

    public async Task<MicrosoftCalendarConnectionStatusResponse>
        CompleteConnectionAsync(
            CompleteMicrosoftCalendarConnectionRequest request,
            CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
            return StatusFailure("Microsoft Calendar konfiqurasiyası tamamlanmayıb.");

        if (await _companyAccessService.ResolveAsync(
                request.EmployerUserId,
                cancellationToken) is null)
        {
            return StatusFailure("Employer hesabı tapılmadı və ya aktiv deyil.");
        }

        if (string.IsNullOrWhiteSpace(request.Code)
            || string.IsNullOrWhiteSpace(request.CodeVerifier)
            || request.CodeVerifier.Length > 160
            || !IsAllowedRedirectUri(request.RedirectUri))
        {
            return StatusFailure("Microsoft callback məlumatları düzgün deyil.");
        }

        TokenResponse token;
        try
        {
            token = await ExchangeAuthorizationCodeAsync(request, cancellationToken);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException
                or InvalidOperationException
                or JsonException)
            || (exception is TaskCanceledException
                && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(
                exception,
                "Employer {EmployerUserId} üçün Microsoft authorization code dəyişdirilmədi.",
                request.EmployerUserId);
            return StatusFailure("Microsoft Outlook bağlantını təsdiqləmədi. Yenidən qoşulun.");
        }

        if (string.IsNullOrWhiteSpace(token.AccessToken)
            || string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            return StatusFailure("Microsoft refresh token qaytarmadı. offline_access icazəsini yoxlayın.");
        }

        MicrosoftProfile profile;
        try
        {
            profile = await GetProfileAsync(token.AccessToken, cancellationToken);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException or JsonException)
            || (exception is TaskCanceledException
                && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(
                exception,
                "Microsoft profile employer {EmployerUserId} üçün alınmadı.",
                request.EmployerUserId);
            return StatusFailure("Microsoft hesabının profil məlumatı oxunmadı.");
        }

        var email = string.IsNullOrWhiteSpace(profile.Mail)
            ? profile.UserPrincipalName
            : profile.Mail;
        if (string.IsNullOrWhiteSpace(profile.Id)
            || string.IsNullOrWhiteSpace(email))
        {
            return StatusFailure("Qoşulan Microsoft hesabında email/calendar mailbox tapılmadı.");
        }

        var now = DateTime.UtcNow;
        var connection = await _dbContext.MicrosoftCalendarConnections
            .SingleOrDefaultAsync(
                item => item.UserId == request.EmployerUserId,
                cancellationToken);

        if (connection is null)
        {
            connection = new MicrosoftCalendarConnection
            {
                UserId = request.EmployerUserId,
                ConnectedAtUtc = now
            };
            _dbContext.MicrosoftCalendarConnections.Add(connection);
        }

        connection.MicrosoftUserId = profile.Id;
        connection.TenantId = NormalizeTenant();
        connection.Email = email.Trim();
        connection.DisplayName = profile.DisplayName?.Trim() ?? string.Empty;
        connection.ProtectedAccessToken = _tokenProtector.Protect(token.AccessToken);
        connection.ProtectedRefreshToken = _tokenProtector.Protect(token.RefreshToken);
        connection.AccessTokenExpiresAtUtc = now.AddSeconds(
            Math.Max(60, token.ExpiresIn));
        connection.GrantedScopes = token.Scope?.Trim()
            ?? string.Join(' ', GetScopes());
        connection.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new MicrosoftCalendarConnectionStatusResponse
        {
            Success = true,
            Message = $"{connection.Email} Outlook calendar-a qoşuldu.",
            IsConfigured = true,
            IsConnected = true,
            Email = connection.Email,
            DisplayName = connection.DisplayName,
            ConnectedAtUtc = connection.ConnectedAtUtc
        };
    }

    public async Task<MicrosoftCalendarConnectionStatusResponse> GetStatusAsync(
        int employerUserId,
        CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0
            || await _companyAccessService.ResolveAsync(
                employerUserId,
                cancellationToken) is null)
        {
            return StatusFailure("Employer hesabı tapılmadı və ya aktiv deyil.");
        }

        var connection = await _dbContext.MicrosoftCalendarConnections
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.UserId == employerUserId,
                cancellationToken);

        return new MicrosoftCalendarConnectionStatusResponse
        {
            Success = true,
            Message = connection is null
                ? "Outlook calendar hələ qoşulmayıb."
                : "Outlook calendar qoşulub.",
            IsConfigured = IsConfigured(),
            IsConnected = connection is not null,
            Email = connection?.Email ?? string.Empty,
            DisplayName = connection?.DisplayName ?? string.Empty,
            ConnectedAtUtc = connection?.ConnectedAtUtc
        };
    }

    public async Task<MicrosoftCalendarConnectionStatusResponse> DisconnectAsync(
        int employerUserId,
        CancellationToken cancellationToken = default)
    {
        if (await _companyAccessService.ResolveAsync(
                employerUserId,
                cancellationToken) is null)
        {
            return StatusFailure("Employer hesabı tapılmadı və ya aktiv deyil.");
        }

        var connection = await _dbContext.MicrosoftCalendarConnections
            .SingleOrDefaultAsync(
                item => item.UserId == employerUserId,
                cancellationToken);
        if (connection is not null)
        {
            _dbContext.MicrosoftCalendarConnections.Remove(connection);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new MicrosoftCalendarConnectionStatusResponse
        {
            Success = true,
            Message = "Outlook calendar bağlantısı silindi.",
            IsConfigured = IsConfigured(),
            IsConnected = false
        };
    }

    public async Task<InterviewAvailabilityResponse> GetAvailabilityAsync(
        InterviewAvailabilityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmployerUserId <= 0
            || request.VacancyId <= 0
            || request.ApplicationId <= 0)
        {
            return AvailabilityFailure(
                "Availability sorğusunda employer, vacancy və application düzgün deyil.");
        }

        var rangeStartUtc = request.RangeStartUtc.UtcDateTime;
        var rangeEndUtc = request.RangeEndUtc.UtcDateTime;
        if (rangeEndUtc <= rangeStartUtc
            || rangeEndUtc - rangeStartUtc > TimeSpan.FromDays(14))
        {
            return AvailabilityFailure(
                "Calendar intervalı düzgün deyil və maksimum 14 gün ola bilər.");
        }

        var access = await _companyAccessService.ResolveAsync(
            request.EmployerUserId,
            cancellationToken);
        if (access is null)
            return AvailabilityFailure("Employer hesabı tapılmadı və ya aktiv deyil.");

        var application = await _dbContext.VacancyApplications
            .AsNoTracking()
            .Include(item => item.Vacancy)
            .SingleOrDefaultAsync(
                item => item.Id == request.ApplicationId
                    && item.VacancyId == request.VacancyId
                    && item.Vacancy.CompanyOwnerUserId == access.CompanyOwnerUserId,
                cancellationToken);
        if (application is null)
            return AvailabilityFailure("Application tapılmadı və ya bu şirkətə aid deyil.");

        var candidate = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == application.CandidateUserId,
                cancellationToken);
        if (candidate is null || string.IsNullOrWhiteSpace(candidate.Email))
            return AvailabilityFailure("Namizədin email ünvanı tapılmadı.");

        var connection = await _dbContext.MicrosoftCalendarConnections
            .SingleOrDefaultAsync(
                item => item.UserId == request.EmployerUserId,
                cancellationToken);
        if (connection is null)
            return AvailabilityFailure("Əvvəl Outlook calendar hesabınızı qoşun.");

        string accessToken;
        try
        {
            accessToken = await GetValidAccessTokenAsync(
                connection,
                cancellationToken);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException
                or InvalidOperationException
                or JsonException)
            || (exception is TaskCanceledException
                && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(
                exception,
                "Employer {EmployerUserId} availability üçün Microsoft token almadı.",
                request.EmployerUserId);
            return AvailabilityFailure(
                "Outlook bağlantısının müddəti bitib. Hesabı ayırıb yenidən qoşun.");
        }

        List<CalendarBusySlotResponse> organizerSlots;
        try
        {
            organizerSlots = await GetOrganizerBusySlotsAsync(
                accessToken,
                rangeStartUtc,
                rangeEndUtc,
                cancellationToken);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException or JsonException)
            || (exception is TaskCanceledException
                && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(
                exception,
                "Employer {EmployerUserId} Outlook calendar view alınmadı.",
                request.EmployerUserId);
            return AvailabilityFailure(
                "HR-ın Outlook calendar məlumatı oxunmadı. Yenidən cəhd edin.");
        }

        var candidateAvailability = await GetCandidateBusySlotsAsync(
            accessToken,
            candidate.Email.Trim(),
            rangeStartUtc,
            rangeEndUtc,
            cancellationToken);
        var candidateName = GetDisplayName(candidate);

        return new InterviewAvailabilityResponse
        {
            Success = true,
            Message = "Outlook availability yükləndi.",
            OrganizerEmail = connection.Email,
            CandidateEmail = candidate.Email.Trim(),
            CandidateName = candidateName,
            CandidateAvailabilityAvailable = candidateAvailability.IsAvailable,
            CandidateAvailabilityMessage = candidateAvailability.Message,
            BusySlots = organizerSlots
                .Concat(candidateAvailability.BusySlots)
                .OrderBy(item => item.StartAtUtc)
                .ThenBy(item => item.Source)
                .ToList()
        };
    }

    public async Task<CreateInterviewMeetingResponse> CreateMeetingAsync(
        CreateInterviewMeetingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmployerUserId <= 0
            || request.VacancyId <= 0
            || request.ApplicationId <= 0)
        {
            return MeetingFailure("Meeting sorğusunda employer, vacancy və application düzgün deyil.");
        }

        var access = await _companyAccessService.ResolveAsync(
            request.EmployerUserId,
            cancellationToken);
        if (access is null)
            return MeetingFailure("Employer hesabı tapılmadı və ya aktiv deyil.");

        var application = await _dbContext.VacancyApplications
            .AsNoTracking()
            .Include(item => item.Vacancy)
            .SingleOrDefaultAsync(
                item => item.Id == request.ApplicationId
                    && item.VacancyId == request.VacancyId
                    && item.Vacancy.CompanyOwnerUserId == access.CompanyOwnerUserId,
                cancellationToken);
        if (application is null)
            return MeetingFailure("Application tapılmadı və ya bu şirkətə aid deyil.");

        var candidate = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Id == application.CandidateUserId,
                cancellationToken);
        if (candidate is null || string.IsNullOrWhiteSpace(candidate.Email))
            return MeetingFailure("Namizədin email ünvanı tapılmadı.");

        var startUtc = request.StartAtUtc.UtcDateTime;
        if (startUtc < DateTime.UtcNow.AddMinutes(2))
            return MeetingFailure("Meeting vaxtı gələcək tarixdə olmalıdır.");
        if (request.DurationMinutes is < 15 or > 480)
            return MeetingFailure("Meeting müddəti 15–480 dəqiqə arasında olmalıdır.");

        var connection = await _dbContext.MicrosoftCalendarConnections
            .SingleOrDefaultAsync(
                item => item.UserId == request.EmployerUserId,
                cancellationToken);
        if (connection is null)
            return MeetingFailure("Əvvəl Outlook calendar hesabınızı qoşun.");

        string accessToken;
        try
        {
            accessToken = await GetValidAccessTokenAsync(connection, cancellationToken);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException
                or InvalidOperationException
                or JsonException)
            || (exception is TaskCanceledException
                && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(
                exception,
                "Employer {EmployerUserId} Microsoft token refresh uğursuz oldu.",
                request.EmployerUserId);
            return MeetingFailure("Outlook bağlantısının müddəti bitib. Hesabı ayırıb yenidən qoşun.");
        }

        var subject = string.IsNullOrWhiteSpace(request.Subject)
            ? $"Interview — {application.Vacancy.RoleTitle}"
            : request.Subject.Trim();
        if (subject.Length > 240)
            return MeetingFailure("Meeting mövzusu maksimum 240 simvol ola bilər.");
        var endUtc = startUtc.AddMinutes(request.DurationMinutes);

        try
        {
            var organizerSlots = await GetOrganizerBusySlotsAsync(
                accessToken,
                startUtc,
                endUtc,
                cancellationToken);
            if (organizerSlots.Any(slot => Overlaps(
                    slot.StartAtUtc,
                    slot.EndAtUtc,
                    startUtc,
                    endUtc)))
            {
                return MeetingFailure(
                    "Seçilən vaxt HR-ın Outlook calendar-ındakı başqa tədbirlə üst-üstə düşür.");
            }
        }
        catch (Exception exception) when (
            (exception is HttpRequestException or JsonException)
            || (exception is TaskCanceledException
                && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(
                exception,
                "Meeting-dən əvvəl employer {EmployerUserId} calendar konflikti yoxlanmadı.",
                request.EmployerUserId);
            return MeetingFailure(
                "HR calendar konflikti yoxlanmadığı üçün meeting yaradılmadı. Yenidən cəhd edin.");
        }

        var candidateAvailability = await GetCandidateBusySlotsAsync(
            accessToken,
            candidate.Email.Trim(),
            startUtc,
            endUtc,
            cancellationToken);
        if (candidateAvailability.IsAvailable
            && candidateAvailability.BusySlots.Any(slot => Overlaps(
                slot.StartAtUtc,
                slot.EndAtUtc,
                startUtc,
                endUtc)))
        {
            return MeetingFailure(
                "Seçilən vaxt namizədin Outlook calendar-ında busy görünür.");
        }

        var transactionId = Guid.NewGuid().ToString();
        var vacancyTitle = string.IsNullOrWhiteSpace(application.Vacancy.RoleTitle)
            ? application.Vacancy.PositionName
            : application.Vacancy.RoleTitle;
        var candidateName = GetDisplayName(candidate);
        var body = BuildMeetingBody(
            request.Agenda,
            vacancyTitle,
            candidateName,
            application.Vacancy.PlatformVacancyId);

        var graphPayload = new Dictionary<string, object?>
        {
            ["subject"] = subject,
            ["body"] = new { contentType = "HTML", content = body },
            ["start"] = new
            {
                dateTime = startUtc.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                timeZone = "UTC"
            },
            ["end"] = new
            {
                dateTime = endUtc.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                timeZone = "UTC"
            },
            ["attendees"] = new[]
            {
                new
                {
                    emailAddress = new
                    {
                        address = candidate.Email.Trim(),
                        name = candidateName
                    },
                    type = "required"
                }
            },
            ["allowNewTimeProposals"] = true,
            ["isOnlineMeeting"] = request.CreateTeamsMeeting,
            ["transactionId"] = transactionId
        };
        if (request.CreateTeamsMeeting)
            graphPayload["onlineMeetingProvider"] = "teamsForBusiness";

        GraphEvent? graphEvent;
        try
        {
            using var graphRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://graph.microsoft.com/v1.0/me/events");
            graphRequest.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            graphRequest.Content = JsonContent.Create(graphPayload, options: JsonOptions);
            using var graphResponse = await _httpClient.SendAsync(
                graphRequest,
                cancellationToken);
            var graphBody = await graphResponse.Content.ReadAsStringAsync(cancellationToken);
            if (!graphResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Microsoft Graph event create failed for employer {EmployerUserId}. HTTP {StatusCode}. Body: {Body}",
                    request.EmployerUserId,
                    (int)graphResponse.StatusCode,
                    Truncate(graphBody, 1000));
                return MeetingFailure(request.CreateTeamsMeeting
                    ? "Microsoft meeting yaratmadı. Qoşulan hesabda Outlook calendar və Teams lisenziyasını yoxlayın."
                    : "Microsoft Outlook calendar event yaratmadı.");
            }

            graphEvent = JsonSerializer.Deserialize<GraphEvent>(graphBody, JsonOptions);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Microsoft Graph event endpoint employer {EmployerUserId} üçün əlçatan olmadı.",
                request.EmployerUserId);
            return MeetingFailure("Microsoft Graph-a qoşulmaq mümkün olmadı. Yenidən cəhd edin.");
        }
        catch (TaskCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                exception,
                "Microsoft Graph event request employer {EmployerUserId} üçün timeout oldu.",
                request.EmployerUserId);
            return MeetingFailure("Microsoft Graph sorğusunun vaxtı bitdi. Yenidən cəhd edin.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(
                exception,
                "Microsoft Graph event response employer {EmployerUserId} üçün oxunmadı.",
                request.EmployerUserId);
            return MeetingFailure("Microsoft event cavabı oxunmadı. Yenidən cəhd edin.");
        }

        if (graphEvent is null || string.IsNullOrWhiteSpace(graphEvent.Id))
            return MeetingFailure("Microsoft event yaratdı, amma event ID qaytarmadı.");

        var meeting = new InterviewMeeting
        {
            VacancyApplicationId = application.Id,
            OrganizerUserId = request.EmployerUserId,
            Subject = subject,
            CandidateEmail = candidate.Email.Trim(),
            StartAtUtc = startUtc,
            EndAtUtc = endUtc,
            IsOnlineMeeting = request.CreateTeamsMeeting,
            GraphEventId = graphEvent.Id,
            WebLink = graphEvent.WebLink ?? string.Empty,
            JoinUrl = graphEvent.OnlineMeeting?.JoinUrl ?? string.Empty,
            TransactionId = transactionId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.InterviewMeetings.Add(meeting);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateInterviewMeetingResponse
        {
            Success = true,
            Message = candidateAvailability.IsAvailable
                ? $"Meeting yaradıldı və dəvət {candidate.Email}-a göndərildi."
                : $"Meeting yaradıldı və dəvət {candidate.Email}-a göndərildi. Namizədin external calendar availability-si Microsoft tərəfindən təqdim edilmədi.",
            MeetingId = meeting.Id,
            CandidateEmail = meeting.CandidateEmail,
            OrganizerEmail = connection.Email,
            StartAtUtc = meeting.StartAtUtc,
            EndAtUtc = meeting.EndAtUtc,
            WebLink = meeting.WebLink,
            JoinUrl = meeting.JoinUrl
        };
    }

    private async Task<List<CalendarBusySlotResponse>> GetOrganizerBusySlotsAsync(
        string accessToken,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken)
    {
        var slots = new List<CalendarBusySlotResponse>();
        string? nextUrl =
            "https://graph.microsoft.com/v1.0/me/calendarView"
            + $"?startDateTime={Uri.EscapeDataString(rangeStartUtc.ToString("O", CultureInfo.InvariantCulture))}"
            + $"&endDateTime={Uri.EscapeDataString(rangeEndUtc.ToString("O", CultureInfo.InvariantCulture))}"
            + "&$select=subject,start,end,isAllDay,isCancelled,showAs"
            + "&$orderby=start/dateTime&$top=250";
        var pageCount = 0;

        while (!string.IsNullOrWhiteSpace(nextUrl) && pageCount < 10)
        {
            pageCount++;
            using var request = new HttpRequestMessage(HttpMethod.Get, nextUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
            request.Headers.TryAddWithoutValidation(
                "Prefer",
                "outlook.timezone=\"UTC\"");
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"Microsoft calendarView HTTP {(int)response.StatusCode}: {Truncate(body, 600)}");
            }

            var page = JsonSerializer.Deserialize<GraphCalendarViewResponse>(
                body,
                JsonOptions)
                ?? throw new JsonException("Microsoft calendarView response boşdur.");

            foreach (var item in page.Value)
            {
                if (item.IsCancelled || !IsBusyStatus(item.ShowAs))
                    continue;
                if (!TryParseGraphUtc(item.Start, out var startUtc)
                    || !TryParseGraphUtc(item.End, out var endUtc)
                    || endUtc <= startUtc)
                {
                    continue;
                }

                startUtc = startUtc < rangeStartUtc ? rangeStartUtc : startUtc;
                endUtc = endUtc > rangeEndUtc ? rangeEndUtc : endUtc;
                if (endUtc <= startUtc)
                    continue;

                slots.Add(new CalendarBusySlotResponse
                {
                    Source = "organizer",
                    Title = string.IsNullOrWhiteSpace(item.Subject)
                        ? "Busy"
                        : item.Subject.Trim(),
                    StartAtUtc = startUtc,
                    EndAtUtc = endUtc,
                    IsAllDay = item.IsAllDay,
                    Status = string.IsNullOrWhiteSpace(item.ShowAs)
                        ? "busy"
                        : item.ShowAs
                });
            }

            nextUrl = page.NextLink;
        }

        return slots;
    }

    private async Task<CandidateAvailabilityResult> GetCandidateBusySlotsAsync(
        string accessToken,
        string candidateEmail,
        DateTime rangeStartUtc,
        DateTime rangeEndUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var payload = new
            {
                schedules = new[] { candidateEmail },
                startTime = new
                {
                    dateTime = rangeStartUtc.ToString(
                        "yyyy-MM-dd'T'HH:mm:ss",
                        CultureInfo.InvariantCulture),
                    timeZone = "UTC"
                },
                endTime = new
                {
                    dateTime = rangeEndUtc.ToString(
                        "yyyy-MM-dd'T'HH:mm:ss",
                        CultureInfo.InvariantCulture),
                    timeZone = "UTC"
                },
                availabilityViewInterval = 15
            };
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "https://graph.microsoft.com/v1.0/me/calendar/getSchedule");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                accessToken);
            request.Headers.TryAddWithoutValidation(
                "Prefer",
                "outlook.timezone=\"UTC\"");
            request.Content = JsonContent.Create(payload, options: JsonOptions);
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Candidate {CandidateEmail} getSchedule unavailable. HTTP {StatusCode}. Body: {Body}",
                    candidateEmail,
                    (int)response.StatusCode,
                    Truncate(body, 600));
                return CandidateAvailabilityResult.Unavailable(
                    "Namizədin free/busy məlumatı Microsoft tərəfindən paylaşılmır. HR calendar-ı tam yoxlanır.");
            }

            var graphResult = JsonSerializer.Deserialize<GraphScheduleResponse>(
                body,
                JsonOptions);
            var schedule = graphResult?.Value.FirstOrDefault();
            if (schedule is null || schedule.Error is not null)
            {
                return CandidateAvailabilityResult.Unavailable(
                    "Namizədin Outlook availability məlumatına giriş yoxdur. HR calendar-ı tam yoxlanır.");
            }

            var slots = new List<CalendarBusySlotResponse>();
            foreach (var item in schedule.ScheduleItems)
            {
                if (!IsBusyStatus(item.Status)
                    || !TryParseGraphUtc(item.Start, out var startUtc)
                    || !TryParseGraphUtc(item.End, out var endUtc)
                    || endUtc <= startUtc)
                {
                    continue;
                }

                startUtc = startUtc < rangeStartUtc ? rangeStartUtc : startUtc;
                endUtc = endUtc > rangeEndUtc ? rangeEndUtc : endUtc;
                if (endUtc <= startUtc)
                    continue;

                slots.Add(CreateCandidateBusySlot(
                    startUtc,
                    endUtc,
                    item.Status));
            }

            if (slots.Count == 0
                && !string.IsNullOrWhiteSpace(schedule.AvailabilityView))
            {
                slots.AddRange(BuildCandidateSlotsFromAvailabilityView(
                    schedule.AvailabilityView,
                    rangeStartUtc,
                    rangeEndUtc));
            }

            return CandidateAvailabilityResult.Available(slots);
        }
        catch (Exception exception) when (
            (exception is HttpRequestException or JsonException)
            || (exception is TaskCanceledException
                && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogInformation(
                exception,
                "Candidate {CandidateEmail} availability alınmadı.",
                candidateEmail);
            return CandidateAvailabilityResult.Unavailable(
                "Namizədin Outlook availability məlumatı hazırda əlçatan deyil. HR calendar-ı tam yoxlanır.");
        }
    }

    private static IEnumerable<CalendarBusySlotResponse>
        BuildCandidateSlotsFromAvailabilityView(
            string availabilityView,
            DateTime rangeStartUtc,
            DateTime rangeEndUtc)
    {
        const int intervalMinutes = 15;
        var result = new List<CalendarBusySlotResponse>();
        var index = 0;
        while (index < availabilityView.Length)
        {
            if (!IsBusyAvailabilityCode(availabilityView[index]))
            {
                index++;
                continue;
            }

            var startIndex = index;
            var statusCode = availabilityView[index];
            while (index < availabilityView.Length
                && IsBusyAvailabilityCode(availabilityView[index]))
            {
                index++;
            }

            var startUtc = rangeStartUtc.AddMinutes(
                startIndex * intervalMinutes);
            var endUtc = rangeStartUtc.AddMinutes(index * intervalMinutes);
            if (endUtc > rangeEndUtc)
                endUtc = rangeEndUtc;
            if (endUtc > startUtc)
            {
                result.Add(CreateCandidateBusySlot(
                    startUtc,
                    endUtc,
                    statusCode switch
                    {
                        '1' => "tentative",
                        '3' => "oof",
                        _ => "busy"
                    }));
            }
        }

        return result;
    }

    private static CalendarBusySlotResponse CreateCandidateBusySlot(
        DateTime startUtc,
        DateTime endUtc,
        string status) =>
        new()
        {
            Source = "candidate",
            Title = "Candidate busy",
            StartAtUtc = startUtc,
            EndAtUtc = endUtc,
            IsAllDay = endUtc - startUtc >= TimeSpan.FromHours(23),
            Status = string.IsNullOrWhiteSpace(status) ? "busy" : status
        };

    private static bool TryParseGraphUtc(
        GraphDateTimeTimeZone? value,
        out DateTime utcValue)
    {
        utcValue = default;
        if (value is null || string.IsNullOrWhiteSpace(value.DateTime))
            return false;

        if (!DateTimeOffset.TryParse(
                value.DateTime,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return false;
        }

        utcValue = parsed.UtcDateTime;
        return true;
    }

    private static bool IsBusyStatus(string? status) =>
        !string.Equals(status, "free", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(
            status,
            "workingElsewhere",
            StringComparison.OrdinalIgnoreCase);

    private static bool IsBusyAvailabilityCode(char value) =>
        value is '1' or '2' or '3';

    private static bool Overlaps(
        DateTime firstStart,
        DateTime firstEnd,
        DateTime secondStart,
        DateTime secondEnd) =>
        firstStart < secondEnd && secondStart < firstEnd;

    private static string GetDisplayName(User user)
    {
        var name = string.Join(' ', new[] { user.Name, user.Surname }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name) ? user.Email : name;
    }

    private async Task<TokenResponse> ExchangeAuthorizationCodeAsync(
        CompleteMicrosoftCalendarConnectionRequest request,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId.Trim(),
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = request.Code,
            ["redirect_uri"] = request.RedirectUri,
            ["code_verifier"] = request.CodeVerifier,
            ["scope"] = string.Join(' ', GetScopes())
        });
        return await RequestTokenAsync(content, cancellationToken);
    }

    private async Task<string> GetValidAccessTokenAsync(
        MicrosoftCalendarConnection connection,
        CancellationToken cancellationToken)
    {
        if (connection.AccessTokenExpiresAtUtc > DateTime.UtcNow.AddMinutes(2))
            return _tokenProtector.Unprotect(connection.ProtectedAccessToken);

        var refreshToken = _tokenProtector.Unprotect(
            connection.ProtectedRefreshToken);
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId.Trim(),
            ["client_secret"] = _options.ClientSecret,
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["scope"] = string.Join(' ', GetScopes())
        });
        var token = await RequestTokenAsync(content, cancellationToken);
        if (string.IsNullOrWhiteSpace(token.AccessToken))
            throw new InvalidOperationException("Microsoft access token qaytarmadı.");

        connection.ProtectedAccessToken = _tokenProtector.Protect(token.AccessToken);
        if (!string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            connection.ProtectedRefreshToken =
                _tokenProtector.Protect(token.RefreshToken);
        }
        connection.AccessTokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(
            Math.Max(60, token.ExpiresIn));
        connection.GrantedScopes = token.Scope?.Trim()
            ?? connection.GrantedScopes;
        connection.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return token.AccessToken;
    }

    private async Task<TokenResponse> RequestTokenAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        var tenant = NormalizeTenant();
        using var response = await _httpClient.PostAsync(
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/oauth2/v2.0/token",
            content,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"Microsoft token endpoint HTTP {(int)response.StatusCode}: {Truncate(body, 600)}");
        return JsonSerializer.Deserialize<TokenResponse>(body, JsonOptions)
            ?? throw new JsonException("Microsoft token response boşdur.");
    }

    private async Task<MicrosoftProfile> GetProfileAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://graph.microsoft.com/v1.0/me?$select=id,displayName,mail,userPrincipalName");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MicrosoftProfile>(
            JsonOptions,
            cancellationToken)
            ?? throw new JsonException("Microsoft profile response boşdur.");
    }

    private bool IsAllowedRedirectUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return false;
        if (_environment.IsDevelopment() && uri.IsLoopback)
            return uri.Scheme is "http" or "https";
        return uri.Scheme == Uri.UriSchemeHttps
            && (uri.Host.Equals("bothfind.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("www.bothfind.com", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(_options.ClientId)
        && !string.IsNullOrWhiteSpace(_options.ClientSecret);

    private string NormalizeTenant() => string.IsNullOrWhiteSpace(_options.Tenant)
        ? "common"
        : _options.Tenant.Trim();

    private IReadOnlyCollection<string> GetScopes() =>
        (_options.Scopes ?? Array.Empty<string>())
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string BuildMeetingBody(
        string agenda,
        string vacancyTitle,
        string candidateName,
        string platformVacancyId)
    {
        var safeAgenda = string.IsNullOrWhiteSpace(agenda)
            ? "Interview details will be discussed during the meeting."
            : WebUtility.HtmlEncode(agenda.Trim())
                .Replace("\r\n", "<br />", StringComparison.Ordinal)
                .Replace("\n", "<br />", StringComparison.Ordinal);
        return $"""
            <p><strong>BothFind interview</strong></p>
            <p>Vacancy: {WebUtility.HtmlEncode(vacancyTitle)} ({WebUtility.HtmlEncode(platformVacancyId)})</p>
            <p>Candidate: {WebUtility.HtmlEncode(candidateName)}</p>
            <p>{safeAgenda}</p>
            """;
    }

    private static MicrosoftCalendarAuthorizationUrlResponse AuthorizationFailure(
        string message) => new() { Success = false, Message = message };

    private MicrosoftCalendarConnectionStatusResponse StatusFailure(string message) =>
        new()
        {
            Success = false,
            Message = message,
            IsConfigured = IsConfigured(),
            IsConnected = false
        };

    private static CreateInterviewMeetingResponse MeetingFailure(string message) =>
        new() { Success = false, Message = message };

    private static InterviewAvailabilityResponse AvailabilityFailure(
        string message) =>
        new() { Success = false, Message = message };

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private sealed class MicrosoftProfile
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? Mail { get; set; }
        public string? UserPrincipalName { get; set; }
    }

    private sealed class GraphCalendarViewResponse
    {
        public List<GraphCalendarEvent> Value { get; set; } = new();

        [JsonPropertyName("@odata.nextLink")]
        public string? NextLink { get; set; }
    }

    private sealed class GraphCalendarEvent
    {
        public string? Subject { get; set; }
        public GraphDateTimeTimeZone? Start { get; set; }
        public GraphDateTimeTimeZone? End { get; set; }
        public bool IsAllDay { get; set; }
        public bool IsCancelled { get; set; }
        public string? ShowAs { get; set; }
    }

    private sealed class GraphScheduleResponse
    {
        public List<GraphScheduleInformation> Value { get; set; } = new();
    }

    private sealed class GraphScheduleInformation
    {
        public string? ScheduleId { get; set; }
        public string? AvailabilityView { get; set; }
        public List<GraphScheduleItem> ScheduleItems { get; set; } = new();
        public GraphScheduleError? Error { get; set; }
    }

    private sealed class GraphScheduleItem
    {
        public string Status { get; set; } = string.Empty;
        public GraphDateTimeTimeZone? Start { get; set; }
        public GraphDateTimeTimeZone? End { get; set; }
    }

    private sealed class GraphScheduleError
    {
        public string? Code { get; set; }
        public string? Message { get; set; }
    }

    private sealed class GraphDateTimeTimeZone
    {
        public string DateTime { get; set; } = string.Empty;
        public string? TimeZone { get; set; }
    }

    private sealed class CandidateAvailabilityResult
    {
        public bool IsAvailable { get; private init; }
        public string Message { get; private init; } = string.Empty;
        public List<CalendarBusySlotResponse> BusySlots { get; private init; } = new();

        public static CandidateAvailabilityResult Available(
            List<CalendarBusySlotResponse> slots) =>
            new()
            {
                IsAvailable = true,
                Message = "Namizədin Outlook free/busy məlumatı göstərilir.",
                BusySlots = slots
            };

        public static CandidateAvailabilityResult Unavailable(string message) =>
            new()
            {
                IsAvailable = false,
                Message = message
            };
    }

    private sealed class GraphEvent
    {
        public string Id { get; set; } = string.Empty;
        public string? WebLink { get; set; }
        public GraphOnlineMeeting? OnlineMeeting { get; set; }
    }

    private sealed class GraphOnlineMeeting
    {
        public string? JoinUrl { get; set; }
    }
}
