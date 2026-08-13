namespace GloryLikeBackend.Models.SkillAndJob;

public class JobFamily
{
    public int Id { get; set; }

    public string JobName { get; set; } = string.Empty;

    public List<Position> Positions { get; set; } = new();
}
