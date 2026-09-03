using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.CompanyTemplates;

public sealed class SaveCompanyTemplateRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string Audience { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [StringLength(250)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(10000)]
    public string Body { get; set; } = string.Empty;
}

public sealed class CompanyTemplateResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public bool CanManageTemplates { get; set; }
    public CompanyTemplateDto? Template { get; set; }
    public List<CompanyTemplateDto> Templates { get; set; } = [];
    public List<string> Variables { get; set; } = [];
}

public sealed class CompanyTemplateDto
{
    public Guid Id { get; set; }
    public string DefaultKey { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime UpdatedAtUtc { get; set; }
}

public static class CompanyTemplateErrorCodes
{
    public const string Validation = "validation";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Conflict = "conflict";
}
