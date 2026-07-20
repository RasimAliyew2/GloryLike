using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.Vacancies;

public sealed class CreateVacancyRequest
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "Employer user ID düzgün deyil.")]
    public int EmployerUserId { get; set; }

    [Required]
    public CreateVacancyPayload Vacancy { get; set; } = new();
}

public sealed class CreateVacancyPayload
{
    [Range(1, int.MaxValue)]
    public int JobFamilyId { get; set; }

    [Range(1, int.MaxValue)]
    public int SeniorityId { get; set; }

    [Range(1, int.MaxValue)]
    public int PositionId { get; set; }

    [Required]
    [StringLength(200)]
    public string RoleTitle { get; set; } = string.Empty;

    [Required]
    [StringLength(40)]
    public string PlatformVacancyId { get; set; } = string.Empty;

    [StringLength(100)]
    public string ClientRequisitionCode { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EmploymentType { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string ExperienceRequired { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string EducationRequirement { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string EducationLevel { get; set; } = string.Empty;

    [Range(0, 1_000_000)]
    public decimal? MinSalary { get; set; }

    [Range(0, 1_000_000)]
    public decimal? MaxSalary { get; set; }

    [StringLength(50)]
    public string PaymentTerms { get; set; } = string.Empty;

    [StringLength(10)]
    public string Currency { get; set; } = string.Empty;

    public bool HideSalary { get; set; }

    [StringLength(5000)]
    public string JobDescription { get; set; } = string.Empty;

    public List<CreateVacancySkillRequirementRequest> SkillRequirements
    {
        get;
        set;
    } = new();

    public List<int> SelectedSkillIds { get; set; } = new();

    [Range(1, 100)]
    public int MinimumVerificationLevel { get; set; } = 70;

    public List<string> Benefits { get; set; } = new();

    [Required]
    public CreateVacancyApplicationRequirementsRequest ApplicationRequirements
    {
        get;
        set;
    } = new();

    public List<CreateVacancyScreeningQuestionRequest> ScreeningQuestions
    {
        get;
        set;
    } = new();

    [Range(0, 100)]
    public int MinimumMatchScore { get; set; }

    [Range(0, 100)]
    public int MinimumTrustScore { get; set; }

    public bool AutoRejectBelowScore { get; set; }
    public bool RequireVerifiedCoreSkills { get; set; }

    [StringLength(5000)]
    public string ScreeningNotes { get; set; } = string.Empty;

    public List<CreateVacancyFunnelStageRequest> FunnelStages
    {
        get;
        set;
    } = new();

    public bool StageApplied { get; set; }
    public bool StageScreening { get; set; }
    public bool StageInterview { get; set; }
    public bool StageOffer { get; set; }

    [Range(0, 100)]
    public int InterviewRounds { get; set; }

    [Range(0, 365)]
    public int ScreeningSlaDays { get; set; }

    [Required]
    [RegularExpression("^(Public|Internal|Anonymous)$")]
    public string Visibility { get; set; } = string.Empty;

    public DateTime? PublishDate { get; set; }
    public DateTime? ApplicationDeadline { get; set; }

    [EmailAddress]
    [StringLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    public bool AllowInternalCandidates { get; set; }
    public bool NotifyMatchingCandidates { get; set; }
    public bool PublishOnSkillMatch { get; set; }
    public bool PublishOnJobSearchAz { get; set; }
    public bool PublishOnPositionAz { get; set; }
    public bool PublishOnBancoAz { get; set; }
    public bool PublishOnBusyAz { get; set; }
    public bool ShareOnTwitter { get; set; }
    public bool ShareOnLinkedIn { get; set; }

    [Range(1, 10)]
    public int PublicationPriority { get; set; }
}

public enum ApplicationRequirementModeRequest
{
    Required = 1,
    Optional = 2,
    Hidden = 3
}

public sealed class CreateVacancyApplicationRequirementsRequest
{
    public ApplicationRequirementModeRequest FullName { get; set; } =
        ApplicationRequirementModeRequest.Required;

    public ApplicationRequirementModeRequest Email { get; set; } =
        ApplicationRequirementModeRequest.Required;

    public ApplicationRequirementModeRequest Phone { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest Location { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest WorkExperience { get; set; } =
        ApplicationRequirementModeRequest.Required;

    public ApplicationRequirementModeRequest CurrentPosition { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest PreviousCompanies { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest Education { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest Certifications { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest Trainings { get; set; } =
        ApplicationRequirementModeRequest.Hidden;

    public ApplicationRequirementModeRequest Languages { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest Tools { get; set; } =
        ApplicationRequirementModeRequest.Hidden;

    public ApplicationRequirementModeRequest LinkedIn { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest GitHub { get; set; } =
        ApplicationRequirementModeRequest.Hidden;

    public ApplicationRequirementModeRequest Portfolio { get; set; } =
        ApplicationRequirementModeRequest.Hidden;

    public ApplicationRequirementModeRequest PersonalWebsite { get; set; } =
        ApplicationRequirementModeRequest.Hidden;

    public ApplicationRequirementModeRequest CoverLetter { get; set; } =
        ApplicationRequirementModeRequest.Optional;

    public ApplicationRequirementModeRequest AdditionalFiles { get; set; } =
        ApplicationRequirementModeRequest.Hidden;

    public List<CreateVacancyCustomFieldRequest> CustomFields { get; set; } =
        new();
}

public sealed class CreateVacancyCustomFieldRequest
{
    [Required]
    [StringLength(100)]
    public string Label { get; set; } = string.Empty;

    public ApplicationRequirementModeRequest Requirement { get; set; } =
        ApplicationRequirementModeRequest.Optional;
}

public sealed class CreateVacancyScreeningQuestionRequest
{
    [Required]
    [StringLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Text|TrueFalse|OneChoice|ShortAnswer|Number|Date)$")]
    public string AnswerType { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Required|KnockOut)$")]
    public string RequirementType { get; set; } = string.Empty;
}

public sealed class CreateVacancyFunnelStageRequest
{
    [Required]
    [StringLength(100)]
    public string StageName { get; set; } = string.Empty;

    [Range(0, 8760)]
    public int Hours { get; set; }

    public bool IsStandard { get; set; }
}

public sealed class CreateVacancySkillRequirementRequest
{
    [Range(1, int.MaxValue)]
    public int SkillId { get; set; }

    [Range(1, 100)]
    public int MinimumVerificationLevel { get; set; }

    [Required]
    [RegularExpression("^(Required|Desirable)$")]
    public string RequirementType { get; set; } = string.Empty;
}
