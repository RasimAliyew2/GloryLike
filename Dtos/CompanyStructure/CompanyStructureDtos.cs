using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.CompanyStructure;

public sealed class SaveCompanyStructureRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    public List<SaveCompanyStructureDepartmentRequest> Departments { get; set; } = new();
}

public sealed class SaveCompanyStructureDepartmentRequest
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    public List<SaveCompanyStructureDivisionRequest> Divisions { get; set; } = new();
}

public sealed class SaveCompanyStructureDivisionRequest
{
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    public List<SaveCompanyStructurePositionRequest> Positions { get; set; } = new();
}

public sealed class SaveCompanyStructurePositionRequest
{
    [Required]
    [StringLength(160)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Seniority { get; set; } = "Not specified";

    [Range(1, 10000)]
    public int Headcount { get; set; } = 1;

    [StringLength(160)]
    public string ReportsTo { get; set; } = string.Empty;
}

public sealed class CompanyStructureResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public List<CompanyStructureDepartmentDto> Departments { get; set; } = new();
}

public sealed class CompanyStructureDepartmentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CompanyStructureDivisionDto> Divisions { get; set; } = new();
}

public sealed class CompanyStructureDivisionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CompanyStructurePositionDto> Positions { get; set; } = new();
}

public sealed class CompanyStructurePositionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Seniority { get; set; } = "Not specified";
    public int Headcount { get; set; } = 1;
    public string ReportsTo { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class CompanyStructureExportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string FileName { get; set; } = "BothFind_Template_OrgStructure.xlsx";
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public static class CompanyStructureErrorCodes
{
    public const string Validation = "validation";
    public const string Forbidden = "forbidden";
    public const string Import = "import";
    public const string Persistence = "persistence";
}
