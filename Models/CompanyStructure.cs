namespace GloryLikeBackend.Models;

public sealed class CompanyStructureDepartment
{
    public int Id { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User CompanyOwnerUser { get; set; } = null!;
    public List<CompanyStructureDivision> Divisions { get; set; } = new();
}

public sealed class CompanyStructureDivision
{
    public int Id { get; set; }
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public CompanyStructureDepartment Department { get; set; } = null!;
    public List<CompanyStructurePosition> Positions { get; set; } = new();
}

public sealed class CompanyStructurePosition
{
    public int Id { get; set; }
    public int DivisionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public CompanyStructureDivision Division { get; set; } = null!;
}
