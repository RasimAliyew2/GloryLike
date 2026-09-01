namespace GloryLikeBackend.Models;

public sealed class CompanyAccessRole
{
    public Guid Id { get; set; }

    public int OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Scope { get; set; } = CompanyAccessRoleScopes.Company;

    public bool IsSystem { get; set; }

    public bool IsFullAccess { get; set; }

    public int CreatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<CompanyAccessRolePermission> Permissions { get; set; }
        = new List<CompanyAccessRolePermission>();
}

public sealed class CompanyAccessRolePermission
{
    public Guid RoleId { get; set; }

    public CompanyAccessRole Role { get; set; } = null!;

    public string PermissionKey { get; set; } = string.Empty;
}

public static class CompanyAccessRoleScopes
{
    public const string Company = "company";
    public const string Departments = "departments";
    public const string Designated = "designated";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Company,
            Departments,
            Designated
        };
}
