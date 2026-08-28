using GloryLikeBackend.Models.Vacancies;

namespace GloryLikeBackend.Models;

public sealed class InterviewMeeting
{
    public int Id { get; set; }

    public int VacancyApplicationId { get; set; }

    public int OrganizerUserId { get; set; }

    public string Subject { get; set; } = string.Empty;

    public string CandidateEmail { get; set; } = string.Empty;

    public DateTime StartAtUtc { get; set; }

    public DateTime EndAtUtc { get; set; }

    public bool IsOnlineMeeting { get; set; }

    public string GraphEventId { get; set; } = string.Empty;

    public string WebLink { get; set; } = string.Empty;

    public string JoinUrl { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public VacancyApplication VacancyApplication { get; set; } = null!;

    public User OrganizerUser { get; set; } = null!;
}
