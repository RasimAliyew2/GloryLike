using GloryLikeBackend.Models.SkillAndJob;
using GloryLikeBackend.Models.Vacancies;

namespace GloryLikeBackend.Models;

public sealed class CompanyHiringPlan
{
    public int Id { get; set; }
    public int CompanyOwnerUserId { get; set; }
    public int CreatedByUserId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public int? JobFamilyId { get; set; }
    public int? PositionId { get; set; }
    public int SeniorityId { get; set; }
    public int Headcount { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime? TargetStartDate { get; set; }
    public string EmploymentType { get; set; } = "Full-time";
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User CompanyOwnerUser { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public JobFamily? JobFamily { get; set; }
    public Position? Position { get; set; }
    public Seniority Seniority { get; set; } = null!;
    public List<Vacancy> Vacancies { get; set; } = new();
}
