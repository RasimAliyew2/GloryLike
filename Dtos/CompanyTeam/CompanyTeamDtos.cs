using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.CompanyTeam;

public sealed class InviteCompanyTeamMemberRequest
{
    [Range(1, int.MaxValue)]
    public int OwnerUserId { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Role { get; set; } = string.Empty;
}

public sealed class CompanyTeamResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public bool CanManageTeam { get; set; }

    public CompanyTeamMemberDto? Member { get; set; }

    public List<CompanyTeamMemberDto> Members { get; set; } = [];
}

public sealed class CompanyTeamMemberDto
{
    public Guid InvitationId { get; set; }

    public int? UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime InvitedAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }

    public bool IsFounder { get; set; }
}

public sealed class ResolveCompanyTeamInvitationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public string? CompanyType { get; set; }

    public string? Industry { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }
}

public static class CompanyTeamErrorCodes
{
    public const string Validation = "validation";

    public const string NotFound = "not_found";

    public const string Expired = "expired";

    public const string AlreadyAccepted = "already_accepted";

    public const string DuplicateEmail = "duplicate_email";

    public const string EmailDeliveryFailed = "email_delivery_failed";

    public const string Conflict = "conflict";

    public const string Forbidden = "forbidden";
}
