namespace GloryLikeBackend.Models.SkillAndJob;

public sealed class PositionSeniority
{
    public int PositionId { get; set; }

    public Position Position { get; set; } = null!;

    public int SeniorityId { get; set; }

    public Seniority Seniority { get; set; } = null!;
}
