using GloryLikeBackend.Dtos.MicrosoftCalendar;
using GloryLikeBackend.Options;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/microsoft-calendar")]
public sealed class MicrosoftCalendarController : ControllerBase
{
    private const string SharedSecretHeader = "X-BothFind-Backend-Secret";
    private readonly IMicrosoftCalendarService _calendarService;
    private readonly string _backendSharedSecret;

    public MicrosoftCalendarController(
        IMicrosoftCalendarService calendarService,
        IOptions<MicrosoftCalendarOptions> options,
        IConfiguration configuration)
    {
        _calendarService = calendarService;
        _backendSharedSecret = string.IsNullOrWhiteSpace(
            options.Value.BackendSharedSecret)
            ? configuration["SocialAuth:BackendSharedSecret"] ?? string.Empty
            : options.Value.BackendSharedSecret;
    }

    [HttpPost("authorization-url")]
    public async Task<ActionResult<MicrosoftCalendarAuthorizationUrlResponse>>
        CreateAuthorizationUrl(
            [FromBody] MicrosoftCalendarAuthorizationUrlRequest request,
            CancellationToken cancellationToken)
    {
        if (!IsTrustedWebApp())
            return Unauthorized(FailureAuthorization());

        var response = await _calendarService.CreateAuthorizationUrlAsync(
            request,
            cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("complete")]
    public async Task<ActionResult<MicrosoftCalendarConnectionStatusResponse>>
        Complete(
            [FromBody] CompleteMicrosoftCalendarConnectionRequest request,
            CancellationToken cancellationToken)
    {
        if (!IsTrustedWebApp())
            return Unauthorized(FailureStatus());

        var response = await _calendarService.CompleteConnectionAsync(
            request,
            cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("status/{employerUserId:int}")]
    public async Task<ActionResult<MicrosoftCalendarConnectionStatusResponse>>
        Status(
            int employerUserId,
            CancellationToken cancellationToken)
    {
        if (!IsTrustedWebApp())
            return Unauthorized(FailureStatus());

        var response = await _calendarService.GetStatusAsync(
            employerUserId,
            cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpDelete("connection/{employerUserId:int}")]
    public async Task<ActionResult<MicrosoftCalendarConnectionStatusResponse>>
        Disconnect(
            int employerUserId,
            CancellationToken cancellationToken)
    {
        if (!IsTrustedWebApp())
            return Unauthorized(FailureStatus());

        var response = await _calendarService.DisconnectAsync(
            employerUserId,
            cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("availability")]
    public async Task<ActionResult<InterviewAvailabilityResponse>> Availability(
        [FromBody] InterviewAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrustedWebApp())
            return Unauthorized(FailureAvailability());

        var response = await _calendarService.GetAvailabilityAsync(
            request,
            cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("meetings")]
    public async Task<ActionResult<CreateInterviewMeetingResponse>>
        CreateMeeting(
            [FromBody] CreateInterviewMeetingRequest request,
            CancellationToken cancellationToken)
    {
        if (!IsTrustedWebApp())
            return Unauthorized(FailureMeeting());

        var response = await _calendarService.CreateMeetingAsync(
            request,
            cancellationToken);
        if (!response.Success)
            return BadRequest(response);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    private bool IsTrustedWebApp()
    {
        if (string.IsNullOrWhiteSpace(_backendSharedSecret)
            || !Request.Headers.TryGetValue(
                SharedSecretHeader,
                out var suppliedValues))
        {
            return false;
        }

        var supplied = suppliedValues.ToString();
        var expectedBytes = Encoding.UTF8.GetBytes(_backendSharedSecret);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length
            && CryptographicOperations.FixedTimeEquals(
                expectedBytes,
                suppliedBytes);
    }

    private static MicrosoftCalendarAuthorizationUrlResponse FailureAuthorization() =>
        new()
        {
            Success = false,
            Message = "Microsoft Calendar WebApp authorization failed."
        };

    private static MicrosoftCalendarConnectionStatusResponse FailureStatus() =>
        new()
        {
            Success = false,
            Message = "Microsoft Calendar WebApp authorization failed."
        };

    private static CreateInterviewMeetingResponse FailureMeeting() =>
        new()
        {
            Success = false,
            Message = "Microsoft Calendar WebApp authorization failed."
        };

    private static InterviewAvailabilityResponse FailureAvailability() =>
        new()
        {
            Success = false,
            Message = "Microsoft Calendar WebApp authorization failed."
        };
}
