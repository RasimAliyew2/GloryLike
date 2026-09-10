using System.ComponentModel.DataAnnotations;
namespace GloryLikeBackend.Dtos.CompanyTemplates;

public sealed class FunnelTemplateStage
{
    [Required, StringLength(100)]
    public string StageName { get; set; } = string.Empty;
    [Range(0, 8760)]
    public int Hours { get; set; }
    [Required, RegularExpression("^(Recruiter|Hiring Manager|HR)$")]
    public string ResponsibleRole { get; set; } = "Recruiter";
}
public sealed class SaveCompanyFunnelRequest
{
    [Range(1, int.MaxValue)] public int ActorUserId { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [StringLength(1000)] public string Description { get; set; } = string.Empty;
    [Required, MinLength(1), MaxLength(20)] public List<FunnelTemplateStage> Stages { get; set; } = [];
}
public sealed class CompanyFunnelDto
{
    public Guid Id { get; set; }
    public string DefaultKey { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<FunnelTemplateStage> Stages { get; set; } = [];
    public DateTime UpdatedAtUtc { get; set; }
}
public sealed class CompanyFunnelResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public bool CanManageTemplates { get; set; }
    public CompanyFunnelDto? Template { get; set; }
    public List<CompanyFunnelDto> Templates { get; set; } = [];
}
