namespace GloryLikeBackend.Models.SkillAndJob;

public class Seniority
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public List<PositionSeniority> PositionLinks { get; set; } = new();
}
