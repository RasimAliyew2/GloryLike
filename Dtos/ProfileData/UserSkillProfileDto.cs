namespace GloryLikeBackend.Dtos.ProfileData;

public class UserSkillProfileDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;

    public int PositionId { get; set; }
    public string PositionName { get; set; } = string.Empty;

    public int SeniorityId { get; set; }
    public string SeniorityName { get; set; } = string.Empty;

    public int JobFamilyId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;

    public string SkillComplexity { get; set; } = "medium";

    // verified | self_declared | absent
    public string Status { get; set; } = "self_declared";
    public bool IsVerified { get; set; }

    public double KnowledgeScore { get; set; }
    public double ExperienceScore { get; set; }
    public double DepthScore { get; set; }
    public double CredibilityScore { get; set; }

    public string TaskComplexity { get; set; } = string.Empty;
    public string OwnershipLevel { get; set; } = string.Empty;
    public string DepthTier { get; set; } = string.Empty;

    public double ContextScore { get; set; }
    public double ComplexityScore { get; set; }
    public double OwnershipScore { get; set; }
    public double ResultScore { get; set; }
}
