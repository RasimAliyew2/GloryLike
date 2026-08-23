using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.EmployerCandidates;

public sealed class EmployerCandidateProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public EmployerCandidateProfileDto? Candidate { get; set; }
}

public sealed class EmployerCandidateProfileDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string About { get; set; } = string.Empty;
    public string ProfileImageDataUrl { get; set; } = string.Empty;
    public string CurrentJobName { get; set; } = string.Empty;
    public List<EmployerCandidateSkillDto> Skills { get; set; } = [];
    public List<EmployerCandidateExperienceDto> Experiences { get; set; } = [];
    public List<CandidateVacancyHistoryDto> VacancyHistory { get; set; } = [];
    public List<CompanyMessageTeamMemberDto> TeamMembers { get; set; } = [];
}

public sealed class EmployerCandidateSkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public int CredibilityScore { get; set; }
}

public sealed class EmployerCandidateExperienceDto
{
    public string CompanyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string StartYear { get; set; } = string.Empty;
    public string EndYear { get; set; } = string.Empty;
}

public sealed class CandidateVacancyHistoryDto
{
    public int VacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string ApplicationStatus { get; set; } = string.Empty;
    public DateTime AppliedAtUtc { get; set; }
}

public sealed class CompanyMessageTeamMemberDto
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public sealed class CompanyMessagingOverviewResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
    public List<CompanyMessageTeamMemberDto> TeamMembers { get; set; } = [];
    public List<CompanyMessageConversationDto> Conversations { get; set; } = [];
}

public sealed class CompanyMessageConversationDto
{
    public int OtherUserId { get; set; }
    public string OtherDisplayName { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public string CandidateDisplayName { get; set; } = string.Empty;
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageAtUtc { get; set; }
    public int UnreadCount { get; set; }
}

public sealed class CompanyMessageThreadResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public List<CompanyMessageDto> Messages { get; set; } = [];
}

public sealed class CompanyMessageActionResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public CompanyMessageDto? Item { get; set; }
}

public sealed class CompanyUnreadCountResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int UnreadCount { get; set; }
}

public sealed class CompanyMessageDto
{
    public int Id { get; set; }
    public int SenderUserId { get; set; }
    public string SenderDisplayName { get; set; } = string.Empty;
    public int RecipientUserId { get; set; }
    public string RecipientDisplayName { get; set; } = string.Empty;
    public int CandidateUserId { get; set; }
    public string CandidateDisplayName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }
}

public sealed class SendCompanyCandidateMessageRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    [Range(1, int.MaxValue)]
    public int RecipientUserId { get; set; }

    [Range(1, int.MaxValue)]
    public int CandidateUserId { get; set; }

    [Required]
    [StringLength(4000, MinimumLength = 1)]
    public string Body { get; set; } = string.Empty;
}

public sealed class MarkCompanyMessageThreadReadRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    [Range(1, int.MaxValue)]
    public int OtherUserId { get; set; }

    [Range(1, int.MaxValue)]
    public int CandidateUserId { get; set; }
}

public static class EmployerCandidateErrorCodes
{
    public const string Validation = "validation";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
}
