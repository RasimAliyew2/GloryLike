namespace GloryLikeBackend.Models.SkillAndJob
{
    public class Seniority
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int JobFamilyId { get; set; }

        public List<Position> Positions { get; set; } = new();

    }
}
