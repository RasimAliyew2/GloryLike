namespace GloryLikeBackend.Models.SkillAndJob;

public class Position
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }

    public JobFamily JobFamily { get; set; } = null!;

    public List<PositionSeniority> SeniorityLinks { get; set; } = new();

    public List<Skill> Skills { get; set; } = new();
}
