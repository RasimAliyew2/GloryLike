using GloryLikeBackend.Dtos.MicrosoftCalendar;

namespace GloryLikeBackend.Services.Interfaces;

public interface IMicrosoftCalendarService
{
    Task<MicrosoftCalendarAuthorizationUrlResponse> CreateAuthorizationUrlAsync(
        MicrosoftCalendarAuthorizationUrlRequest request,
        CancellationToken cancellationToken = default);

    Task<MicrosoftCalendarConnectionStatusResponse> CompleteConnectionAsync(
        CompleteMicrosoftCalendarConnectionRequest request,
        CancellationToken cancellationToken = default);

    Task<MicrosoftCalendarConnectionStatusResponse> GetStatusAsync(
        int employerUserId,
        CancellationToken cancellationToken = default);

    Task<MicrosoftCalendarConnectionStatusResponse> DisconnectAsync(
        int employerUserId,
        CancellationToken cancellationToken = default);

    Task<InterviewAvailabilityResponse> GetAvailabilityAsync(
        InterviewAvailabilityRequest request,
        CancellationToken cancellationToken = default);

    Task<CreateInterviewMeetingResponse> CreateMeetingAsync(
        CreateInterviewMeetingRequest request,
        CancellationToken cancellationToken = default);
}
