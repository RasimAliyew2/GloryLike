namespace GloryLikeBackend.Models.Ai;

public class SkillQuestionnaire
{
    public Guid Id { get; set; }

    public int? SkillId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string Seniority { get; set; } = string.Empty;

    public string SkillComplexity { get; set; } = string.Empty;

    public string Language { get; set; } = "az";

    public int QuestionCount { get; set; }

    public string StructureJson { get; set; } = string.Empty;

    public int Version { get; set; } = 1;

    public string? GeneratedByModel { get; set; }

    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
