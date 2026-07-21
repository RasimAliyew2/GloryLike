namespace GloryLikeBackend.Models.Vacancies;

public sealed class Vacancy
{
    public int Id { get; set; }
    public int EmployerUserId { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public int JobFamilyId { get; set; }
    public int SeniorityId { get; set; }
    public int PositionId { get; set; }
    public string JobFamilyName { get; set; } = string.Empty;
    public string SeniorityName { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string ClientRequisitionCode { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string ExperienceRequired { get; set; } = string.Empty;
    public string EducationRequirement { get; set; } = string.Empty;
    public string EducationLevel { get; set; } = string.Empty;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public bool HideSalary { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public int MinimumVerificationLevel { get; set; }
    public int MinimumMatchScore { get; set; }
    public int MinimumTrustScore { get; set; }
    public bool AutoRejectBelowScore { get; set; }
    public bool RequireVerifiedCoreSkills { get; set; }
    public string ScreeningNotes { get; set; } = string.Empty;
    public bool StageApplied { get; set; }
    public bool StageScreening { get; set; }
    public bool StageInterview { get; set; }
    public bool StageOffer { get; set; }
    public int InterviewRounds { get; set; }
    public int ScreeningSlaDays { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public bool AllowInternalCandidates { get; set; }
    public bool NotifyMatchingCandidates { get; set; }
    public int PublicationPriority { get; set; }
    public string Status { get; set; } = "Published";
    public string SourcePayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<VacancyApplication> Applications { get; set; } = new();

    public List<VacancySkillRequirement> SkillRequirements { get; set; } =
        new();

    public List<VacancyBenefit> Benefits { get; set; } = new();

    public List<VacancyApplicationRequirement> ApplicationRequirements
    {
        get;
        set;
    } = new();

    public List<VacancyScreeningQuestion> ScreeningQuestions { get; set; } =
        new();

    public List<VacancyFunnelStage> FunnelStages { get; set; } = new();

    public List<VacancyPublicationChannel> PublicationChannels { get; set; } =
        new();
}

public static class VacancyApplicationStatuses
{
    public const string NoResponseYet = "NoResponseYet";
}

public sealed class VacancyApplication
{
    public int Id { get; set; }
    public int VacancyId { get; set; }
    public int CandidateUserId { get; set; }
    public string Status { get; set; } =
        VacancyApplicationStatuses.NoResponseYet;
    public DateTime AppliedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
}

public sealed class VacancySkillRequirement
{
    public int Id { get; set; }
    public int VacancyId { get; set; }
    public int SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public int MinimumVerificationLevel { get; set; }
    public string RequirementType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
}

public sealed class VacancyBenefit
{
    public int Id { get; set; }
    public int VacancyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
}

public sealed class VacancyApplicationRequirement
{
    public int Id { get; set; }
    public int VacancyId { get; set; }
    public string FieldKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string RequirementMode { get; set; } = string.Empty;
    public bool IsCustom { get; set; }
    public int SortOrder { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
}

public sealed class VacancyScreeningQuestion
{
    public int Id { get; set; }
    public int VacancyId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string AnswerType { get; set; } = string.Empty;
    public string RequirementType { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
}

public sealed class VacancyFunnelStage
{
    public int Id { get; set; }
    public int VacancyId { get; set; }
    public string StageName { get; set; } = string.Empty;
    public int Hours { get; set; }
    public bool IsStandard { get; set; }
    public int SortOrder { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
}

public sealed class VacancyPublicationChannel
{
    public int Id { get; set; }
    public int VacancyId { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public int SortOrder { get; set; }
    public Vacancy Vacancy { get; set; } = null!;
}
