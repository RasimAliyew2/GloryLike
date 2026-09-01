namespace GloryLikeBackend.Models;

public sealed class CompanyAccessAuditEvent
{
    public Guid Id { get; set; }

    public int OwnerUserId { get; set; }

    public int ActorUserId { get; set; }

    public int? TargetUserId { get; set; }

    public Guid? RoleId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}

public static class CompanyAccessAuditEventTypes
{
    public const string RoleCreated = "role_created";
    public const string RoleUpdated = "role_updated";
    public const string PermissionGranted = "permission_granted";
    public const string PermissionRevoked = "permission_revoked";
    public const string AccessGranted = "access_granted";
    public const string AccessChanged = "access_changed";
    public const string AccessRevoked = "access_revoked";
}
