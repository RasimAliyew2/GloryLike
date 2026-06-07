namespace GloryLikeBackend.Models.SkillAndJob
{
    public class Position
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int SeniorityId { get; set; }

        public List<Skill> Skills { get; set; } = new();

    }
}
