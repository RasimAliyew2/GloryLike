namespace GloryLikeBackend.Dtos.TalentRadar;

public sealed class TalentRadarResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int TotalVacancies { get; set; }
    public int ScoredVacancies { get; set; }
    public List<TalentRadarCandidateDto> Candidates { get; set; } = new();
}

public sealed class TalentRadarCandidateDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public int BestVacancyId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string TargetRoleTitle { get; set; } = string.Empty;
    public double RoleReadiness { get; set; }
    public int MatchedVacancyCount { get; set; }
    public int MatchedSkillsCount { get; set; }
    public int TemplateSkillsCount { get; set; }
    public List<TalentRadarSkillDto> Skills { get; set; } = new();
}

public sealed class TalentRadarSkillDto
{
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int Score { get; set; }
    public bool IsVerified { get; set; }
}
