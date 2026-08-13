namespace GloryLikeBackend.Models.SkillAndJob;

public sealed class SkillLookupDto
{
    public int Id { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;

    public List<SeniorityOptionDto> Seniorities { get; set; } = new();
}
