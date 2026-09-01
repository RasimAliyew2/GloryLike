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

    public Guid? RoleId { get; set; }

    [StringLength(80)]
    public string Role { get; set; } = string.Empty;
}

public sealed class CompanyTeamResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public string CompanyName { get; set; } = string.Empty;

    public bool CanManageTeam { get; set; }

    public bool CanManageRoles { get; set; }

    public bool CanInvite { get; set; }

    public string ActorRole { get; set; } = string.Empty;

    public CompanyTeamMemberDto? Member { get; set; }

    public List<CompanyTeamMemberDto> Members { get; set; } = [];

    public List<CompanyAccessRoleDto> Roles { get; set; } = [];

    public List<CompanyAccessAuditEventDto> History { get; set; } = [];

    public List<CompanyPermissionGroupDto> PermissionGroups { get; set; } = [];
}

public sealed class CompanyTeamMemberDto
{
    public Guid InvitationId { get; set; }

    public int? UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public Guid? RoleId { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime InvitedAtUtc { get; set; }

    public DateTime? AcceptedAtUtc { get; set; }

    public bool IsFounder { get; set; }

    public bool CanChangeRole { get; set; }

    public bool CanRemove { get; set; }

    public List<string> AllowedRoles { get; set; } = [];
}

public sealed class UpdateCompanyTeamMemberRoleRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    public Guid? RoleId { get; set; }

    [StringLength(80)]
    public string Role { get; set; } = string.Empty;
}

public sealed class SaveCompanyAccessRoleRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Scope { get; set; } = string.Empty;

    public List<string> PermissionKeys { get; set; } = [];
}

public sealed class CompanyAccessRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public bool IsFullAccess { get; set; }
    public int ParticipantCount { get; set; }
    public List<string> PermissionKeys { get; set; } = [];
}

public sealed class CompanyAccessAuditEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public int ActorUserId { get; set; }
    public string ActorName { get; set; } = string.Empty;
    public string ActorEmail { get; set; } = string.Empty;
    public int? TargetUserId { get; set; }
    public string TargetName { get; set; } = string.Empty;
    public string TargetEmail { get; set; } = string.Empty;
    public Guid? RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CompanyPermissionGroupDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public List<CompanyPermissionDto> Permissions { get; set; } = [];
}

public sealed class CompanyPermissionDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool Sensitive { get; set; }
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
