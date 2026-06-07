namespace GloryLikeBackend.Models.SkillAndJob
{
    public class JobFamily
    {
        public int Id { get; set; }

        public string JobName { get; set; }

        public List<Seniority> Seniorities { get; set; } = new();
    }
}
