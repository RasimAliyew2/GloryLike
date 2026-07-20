using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Vacancies;
using GloryLikeBackend.Models.SkillAndJob;
using GloryLikeBackend.Models.Vacancies;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class VacancyService : IVacancyService
{
    private const int MaximumSkillCount = 100;
    private const int MaximumBenefitCount = 50;
    private const int MaximumCustomFieldCount = 20;
    private const int MaximumScreeningQuestionCount = 20;
    private const int MaximumFunnelStageCount = 20;

    private static readonly HashSet<string> VisibilityValues =
        new(StringComparer.Ordinal)
        {
            "Public",
            "Internal",
            "Anonymous"
        };

    private static readonly HashSet<string> SkillRequirementTypes =
        new(StringComparer.Ordinal)
        {
            "Required",
            "Desirable"
        };

    private static readonly HashSet<string> ScreeningAnswerTypes =
        new(StringComparer.Ordinal)
        {
            "Text",
            "TrueFalse",
            "OneChoice",
            "ShortAnswer",
            "Number",
            "Date"
        };

    private static readonly HashSet<string> ScreeningRequirementTypes =
        new(StringComparer.Ordinal)
        {
            "Required",
            "KnockOut"
        };

    private static readonly JsonSerializerOptions PayloadJsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;
    private readonly ILogger<VacancyService> _logger;

    public VacancyService(
        AppDbContext dbContext,
        ILogger<VacancyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CreateVacancyResult> CreateAsync(
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default)
    {
        var payload = request.Vacancy ?? new CreateVacancyPayload();

        Normalize(payload);

        var payloadValidationMessage = ValidatePayload(payload);

        if (!string.IsNullOrWhiteSpace(payloadValidationMessage))
            return CreateVacancyResult.Invalid(payloadValidationMessage);

        var employerExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == request.EmployerUserId,
                cancellationToken);

        if (!employerExists)
        {
            return CreateVacancyResult.Invalid(
                "Employer user SQL-də tapılmadı.");
        }

        var duplicateVacancy = await _dbContext.Vacancies
            .AsNoTracking()
            .AnyAsync(
                vacancy =>
                    vacancy.PlatformVacancyId
                    == payload.PlatformVacancyId,
                cancellationToken);

        if (duplicateVacancy)
        {
            return CreateVacancyResult.Conflict(
                $"{payload.PlatformVacancyId} ID-li vacancy artıq mövcuddur.");
        }

        var jobFamily = await _dbContext.JobFamilies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == payload.JobFamilyId,
                cancellationToken);

        if (jobFamily is null)
        {
            return CreateVacancyResult.Invalid(
                "Seçilən Job Family SQL taxonomy-də tapılmadı.");
        }

        var seniority = await _dbContext.Seniorities
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == payload.SeniorityId
                    && item.JobFamilyId == payload.JobFamilyId,
                cancellationToken);

        if (seniority is null)
        {
            return CreateVacancyResult.Invalid(
                "Seçilən Seniority bu Job Family-yə aid deyil.");
        }

        var position = await _dbContext.Positions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item =>
                    item.Id == payload.PositionId
                    && item.SeniorityId == payload.SeniorityId,
                cancellationToken);

        if (position is null)
        {
            return CreateVacancyResult.Invalid(
                "Seçilən Position bu Seniority-yə aid deyil.");
        }

        var requestedSkillIds = payload.SkillRequirements
            .Select(requirement => requirement.SkillId)
            .ToList();

        var skillsById = await _dbContext.Skills
            .AsNoTracking()
            .Where(skill => requestedSkillIds.Contains(skill.Id))
            .ToDictionaryAsync(
                skill => skill.Id,
                cancellationToken);

        var missingSkillId = requestedSkillIds
            .FirstOrDefault(skillId => !skillsById.ContainsKey(skillId));

        if (missingSkillId > 0)
        {
            return CreateVacancyResult.Invalid(
                $"SkillId {missingSkillId} SQL taxonomy-də tapılmadı.");
        }

        var now = DateTime.UtcNow;

        var vacancy = new Vacancy
        {
            EmployerUserId = request.EmployerUserId,
            PlatformVacancyId = payload.PlatformVacancyId,
            JobFamilyId = payload.JobFamilyId,
            SeniorityId = payload.SeniorityId,
            PositionId = payload.PositionId,
            JobFamilyName = jobFamily.JobName.Trim(),
            SeniorityName = seniority.Name.Trim(),
            PositionName = position.Name.Trim(),
            RoleTitle = payload.RoleTitle,
            ClientRequisitionCode = payload.ClientRequisitionCode,
            EmploymentType = payload.EmploymentType,
            ExperienceRequired = payload.ExperienceRequired,
            EducationRequirement = payload.EducationRequirement,
            EducationLevel = payload.EducationLevel,
            MinSalary = payload.MinSalary,
            MaxSalary = payload.MaxSalary,
            PaymentTerms = payload.PaymentTerms,
            Currency = payload.Currency,
            HideSalary = payload.HideSalary,
            JobDescription = payload.JobDescription,
            MinimumVerificationLevel = payload.MinimumVerificationLevel,
            MinimumMatchScore = payload.MinimumMatchScore,
            MinimumTrustScore = payload.MinimumTrustScore,
            AutoRejectBelowScore = payload.AutoRejectBelowScore,
            RequireVerifiedCoreSkills = payload.RequireVerifiedCoreSkills,
            ScreeningNotes = payload.ScreeningNotes,
            StageApplied = payload.StageApplied,
            StageScreening = payload.StageScreening,
            StageInterview = payload.StageInterview,
            StageOffer = payload.StageOffer,
            InterviewRounds = payload.InterviewRounds,
            ScreeningSlaDays = payload.ScreeningSlaDays,
            Visibility = payload.Visibility,
            PublishDate = payload.PublishDate,
            ApplicationDeadline = payload.ApplicationDeadline,
            ContactEmail = payload.ContactEmail,
            AllowInternalCandidates = payload.AllowInternalCandidates,
            NotifyMatchingCandidates = payload.NotifyMatchingCandidates,
            PublicationPriority = payload.PublicationPriority,
            Status = "Published",
            SourcePayloadJson = JsonSerializer.Serialize(
                payload,
                PayloadJsonOptions),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        AddSkillRequirements(
            vacancy,
            payload,
            skillsById);

        AddBenefits(vacancy, payload);
        AddApplicationRequirements(vacancy, payload);
        AddScreeningQuestions(vacancy, payload);
        AddFunnelStages(vacancy, payload);
        AddPublicationChannels(vacancy, payload);

        _dbContext.Vacancies.Add(vacancy);

        try
        {
            // Bütün aggregate bir SaveChanges çağırışında yazılır.
            // EF Core bunu avtomatik SQL transaction daxilində icra edir.
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(
                ex,
                "Vacancy {PlatformVacancyId} SQL-ə yazılmadı.",
                payload.PlatformVacancyId);

            var platformIdExists = await _dbContext.Vacancies
                .AsNoTracking()
                .AnyAsync(
                    item =>
                        item.PlatformVacancyId
                        == payload.PlatformVacancyId,
                    cancellationToken);

            if (platformIdExists)
            {
                return CreateVacancyResult.Conflict(
                    $"{payload.PlatformVacancyId} ID-li vacancy artıq mövcuddur.");
            }

            throw;
        }

        return CreateVacancyResult.Created(
            vacancy.Id,
            vacancy.PlatformVacancyId,
            vacancy.CreatedAtUtc);
    }

    private static void Normalize(CreateVacancyPayload payload)
    {
        payload.RoleTitle = payload.RoleTitle?.Trim() ?? string.Empty;
        payload.PlatformVacancyId =
            payload.PlatformVacancyId?.Trim()
            ?? string.Empty;
        payload.ClientRequisitionCode =
            payload.ClientRequisitionCode?.Trim()
            ?? string.Empty;
        payload.EmploymentType =
            payload.EmploymentType?.Trim()
            ?? string.Empty;
        payload.ExperienceRequired =
            payload.ExperienceRequired?.Trim()
            ?? string.Empty;
        payload.EducationRequirement =
            payload.EducationRequirement?.Trim()
            ?? string.Empty;
        payload.EducationLevel =
            payload.EducationLevel?.Trim()
            ?? string.Empty;
        payload.PaymentTerms =
            payload.PaymentTerms?.Trim()
            ?? string.Empty;
        payload.Currency =
            payload.Currency?.Trim().ToUpperInvariant()
            ?? string.Empty;
        payload.JobDescription =
            payload.JobDescription?.Trim()
            ?? string.Empty;
        payload.ScreeningNotes =
            payload.ScreeningNotes?.Trim()
            ?? string.Empty;
        payload.Visibility =
            payload.Visibility?.Trim()
            ?? string.Empty;
        payload.ContactEmail =
            payload.ContactEmail?.Trim()
            ?? string.Empty;

        payload.SkillRequirements ??=
            new List<CreateVacancySkillRequirementRequest>();
        payload.SelectedSkillIds ??= new List<int>();
        payload.Benefits ??= new List<string>();
        payload.ApplicationRequirements ??=
            new CreateVacancyApplicationRequirementsRequest();
        payload.ApplicationRequirements.CustomFields ??=
            new List<CreateVacancyCustomFieldRequest>();
        payload.ScreeningQuestions ??=
            new List<CreateVacancyScreeningQuestionRequest>();
        payload.FunnelStages ??=
            new List<CreateVacancyFunnelStageRequest>();

        payload.SkillRequirements = payload.SkillRequirements
            .OfType<CreateVacancySkillRequirementRequest>()
            .ToList();
        payload.ApplicationRequirements.CustomFields =
            payload.ApplicationRequirements.CustomFields
                .OfType<CreateVacancyCustomFieldRequest>()
                .ToList();
        payload.ScreeningQuestions = payload.ScreeningQuestions
            .OfType<CreateVacancyScreeningQuestionRequest>()
            .ToList();
        payload.FunnelStages = payload.FunnelStages
            .OfType<CreateVacancyFunnelStageRequest>()
            .ToList();

        foreach (var skill in payload.SkillRequirements)
        {
            skill.RequirementType =
                skill.RequirementType?.Trim()
                ?? string.Empty;
        }

        payload.SelectedSkillIds = payload.SkillRequirements
            .Select(requirement => requirement.SkillId)
            .Distinct()
            .ToList();

        if (payload.SkillRequirements.Count > 0)
        {
            payload.MinimumVerificationLevel =
                payload.SkillRequirements[0]
                    .MinimumVerificationLevel;
        }

        payload.Benefits = payload.Benefits
            .Where(benefit => !string.IsNullOrWhiteSpace(benefit))
            .Select(benefit => benefit.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var customField in
                 payload.ApplicationRequirements.CustomFields)
        {
            customField.Label =
                customField.Label?.Trim()
                ?? string.Empty;
        }

        foreach (var question in payload.ScreeningQuestions)
        {
            question.QuestionText =
                question.QuestionText?.Trim()
                ?? string.Empty;
            question.AnswerType =
                question.AnswerType?.Trim()
                ?? string.Empty;
            question.RequirementType =
                question.RequirementType?.Trim()
                ?? string.Empty;
        }

        foreach (var stage in payload.FunnelStages)
        {
            stage.StageName =
                stage.StageName?.Trim()
                ?? string.Empty;
        }

        payload.StageApplied = payload.FunnelStages.Any(
            stage =>
                stage.StageName.Equals(
                    "Applied",
                    StringComparison.OrdinalIgnoreCase)
                || stage.StageName.Equals(
                    "Applications",
                    StringComparison.OrdinalIgnoreCase)
                || stage.StageName.Equals(
                    "Responses",
                    StringComparison.OrdinalIgnoreCase));

        payload.StageScreening = payload.FunnelStages.Any(
            stage => stage.StageName.Contains(
                "Screening",
                StringComparison.OrdinalIgnoreCase));

        payload.StageInterview = payload.FunnelStages.Any(
            stage => stage.StageName.Contains(
                "Interview",
                StringComparison.OrdinalIgnoreCase));

        payload.StageOffer = payload.FunnelStages.Any(
            stage => stage.StageName.Contains(
                "Offer",
                StringComparison.OrdinalIgnoreCase));

        payload.PublishOnSkillMatch = true;
    }

    private static string? ValidatePayload(CreateVacancyPayload payload)
    {
        if (payload.SkillRequirements.Count == 0)
            return "Ən azı bir skill requirement göndərilməlidir.";

        if (payload.SkillRequirements.Count > MaximumSkillCount)
            return $"Maksimum {MaximumSkillCount} skill göndərilə bilər.";

        if (payload.Benefits.Count > MaximumBenefitCount)
            return $"Maksimum {MaximumBenefitCount} benefit göndərilə bilər.";

        if (payload.Benefits.Any(benefit => benefit.Length > 100))
            return "Benefit adı maksimum 100 simvol ola bilər.";

        if (payload.ApplicationRequirements.CustomFields.Count
            > MaximumCustomFieldCount)
        {
            return $"Maksimum {MaximumCustomFieldCount} custom application field göndərilə bilər.";
        }

        if (payload.ScreeningQuestions.Count
            > MaximumScreeningQuestionCount)
        {
            return $"Maksimum {MaximumScreeningQuestionCount} screening sualı göndərilə bilər.";
        }

        if (payload.FunnelStages.Count == 0)
            return "Ən azı bir funnel mərhələsi göndərilməlidir.";

        if (payload.FunnelStages.Count > MaximumFunnelStageCount)
        {
            return $"Maksimum {MaximumFunnelStageCount} funnel mərhələsi göndərilə bilər.";
        }

        if (payload.MinSalary.HasValue
            && payload.MaxSalary.HasValue
            && payload.MinSalary.Value > payload.MaxSalary.Value)
        {
            return "Maximum salary Minimum salary-dən az ola bilməz.";
        }

        if (payload.PublishDate.HasValue
            && payload.ApplicationDeadline.HasValue
            && payload.ApplicationDeadline.Value.Date
                < payload.PublishDate.Value.Date)
        {
            return "Application deadline publish date-dən əvvəl ola bilməz.";
        }

        if (!VisibilityValues.Contains(payload.Visibility))
        {
            return "Publication type Public, Internal və ya Anonymous olmalıdır.";
        }

        var duplicateSkill = payload.SkillRequirements
            .GroupBy(requirement => requirement.SkillId)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateSkill is not null)
        {
            return $"SkillId {duplicateSkill.Key} yalnız bir dəfə göndərilə bilər.";
        }

        foreach (var skill in payload.SkillRequirements)
        {
            if (skill.SkillId <= 0)
                return "Skill ID düzgün deyil.";

            if (skill.MinimumVerificationLevel is < 1 or > 100)
                return "Skill verification level 1–100 arasında olmalıdır.";

            if (!SkillRequirementTypes.Contains(skill.RequirementType))
                return "Skill statusu Required və ya Desirable olmalıdır.";
        }

        var duplicateCustomField = payload.ApplicationRequirements.CustomFields
            .Where(field => !string.IsNullOrWhiteSpace(field.Label))
            .GroupBy(
                field => field.Label,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateCustomField is not null)
        {
            return $"“{duplicateCustomField.Key}” custom field-i yalnız bir dəfə göndərilə bilər.";
        }

        if (GetApplicationRequirementModes(payload.ApplicationRequirements)
            .Any(mode => !Enum.IsDefined(mode)))
        {
            return "Application requirement mode düzgün deyil.";
        }

        if (payload.ApplicationRequirements.CustomFields.Any(
            field =>
                string.IsNullOrWhiteSpace(field.Label)
                || field.Label.Length > 100
                || !Enum.IsDefined(field.Requirement)))
        {
            return "Custom application field düzgün deyil.";
        }

        foreach (var question in payload.ScreeningQuestions)
        {
            if (string.IsNullOrWhiteSpace(question.QuestionText))
                return "Screening sualının mətni boş ola bilməz.";

            if (question.QuestionText.Length > 500)
                return "Screening sualı maksimum 500 simvol ola bilər.";

            if (!ScreeningAnswerTypes.Contains(question.AnswerType))
                return "Screening answer type düzgün deyil.";

            if (!ScreeningRequirementTypes.Contains(question.RequirementType))
                return "Screening requirement type düzgün deyil.";
        }

        var duplicateFunnelStage = payload.FunnelStages
            .Where(stage => !string.IsNullOrWhiteSpace(stage.StageName))
            .GroupBy(
                stage => stage.StageName,
                StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateFunnelStage is not null)
        {
            return $"“{duplicateFunnelStage.Key}” funnel mərhələsi yalnız bir dəfə göndərilə bilər.";
        }

        if (payload.FunnelStages.Any(
            stage =>
                string.IsNullOrWhiteSpace(stage.StageName)
                || stage.StageName.Length > 100
                || stage.Hours is < 0 or > 8760))
        {
            return "Funnel mərhələsinin adı və ya saatı düzgün deyil.";
        }

        return null;
    }

    private static IEnumerable<ApplicationRequirementModeRequest>
        GetApplicationRequirementModes(
            CreateVacancyApplicationRequirementsRequest requirements)
    {
        yield return requirements.FullName;
        yield return requirements.Email;
        yield return requirements.Phone;
        yield return requirements.Location;
        yield return requirements.WorkExperience;
        yield return requirements.CurrentPosition;
        yield return requirements.PreviousCompanies;
        yield return requirements.Education;
        yield return requirements.Certifications;
        yield return requirements.Trainings;
        yield return requirements.Languages;
        yield return requirements.Tools;
        yield return requirements.LinkedIn;
        yield return requirements.GitHub;
        yield return requirements.Portfolio;
        yield return requirements.PersonalWebsite;
        yield return requirements.CoverLetter;
        yield return requirements.AdditionalFiles;
    }

    private static void AddSkillRequirements(
        Vacancy vacancy,
        CreateVacancyPayload payload,
        IReadOnlyDictionary<int, Skill> skillsById)
    {
        for (var index = 0; index < payload.SkillRequirements.Count; index++)
        {
            var requirement = payload.SkillRequirements[index];
            var skill = skillsById[requirement.SkillId];

            vacancy.SkillRequirements.Add(
                new VacancySkillRequirement
                {
                    Vacancy = vacancy,
                    SkillId = requirement.SkillId,
                    SkillName = skill.SkillName.Trim(),
                    MinimumVerificationLevel =
                        requirement.MinimumVerificationLevel,
                    RequirementType = requirement.RequirementType,
                    SortOrder = index
                });
        }
    }

    private static void AddBenefits(
        Vacancy vacancy,
        CreateVacancyPayload payload)
    {
        for (var index = 0; index < payload.Benefits.Count; index++)
        {
            vacancy.Benefits.Add(
                new VacancyBenefit
                {
                    Vacancy = vacancy,
                    Name = payload.Benefits[index],
                    SortOrder = index
                });
        }
    }

    private static void AddApplicationRequirements(
        Vacancy vacancy,
        CreateVacancyPayload payload)
    {
        var requirements = payload.ApplicationRequirements;

        var standardFields = new[]
        {
            ("fullName", "Full Name", requirements.FullName),
            ("email", "Email", requirements.Email),
            ("phone", "Phone", requirements.Phone),
            ("location", "Location", requirements.Location),
            ("workExperience", "Work Experience", requirements.WorkExperience),
            ("currentPosition", "Current Position", requirements.CurrentPosition),
            ("previousCompanies", "Previous Companies", requirements.PreviousCompanies),
            ("education", "Education", requirements.Education),
            ("certifications", "Certifications", requirements.Certifications),
            ("trainings", "Trainings", requirements.Trainings),
            ("languages", "Languages", requirements.Languages),
            ("tools", "Tools", requirements.Tools),
            ("linkedIn", "LinkedIn", requirements.LinkedIn),
            ("gitHub", "GitHub", requirements.GitHub),
            ("portfolio", "Portfolio", requirements.Portfolio),
            ("personalWebsite", "Personal Website", requirements.PersonalWebsite),
            ("coverLetter", "Cover Letter", requirements.CoverLetter),
            ("additionalFiles", "Additional Files", requirements.AdditionalFiles)
        };

        for (var index = 0; index < standardFields.Length; index++)
        {
            var field = standardFields[index];

            vacancy.ApplicationRequirements.Add(
                new VacancyApplicationRequirement
                {
                    Vacancy = vacancy,
                    FieldKey = field.Item1,
                    Label = field.Item2,
                    RequirementMode = field.Item3.ToString(),
                    IsCustom = false,
                    SortOrder = index
                });
        }

        for (var index = 0; index < requirements.CustomFields.Count; index++)
        {
            var field = requirements.CustomFields[index];

            vacancy.ApplicationRequirements.Add(
                new VacancyApplicationRequirement
                {
                    Vacancy = vacancy,
                    FieldKey = $"custom-{index + 1}",
                    Label = field.Label,
                    RequirementMode = field.Requirement.ToString(),
                    IsCustom = true,
                    SortOrder = standardFields.Length + index
                });
        }
    }

    private static void AddScreeningQuestions(
        Vacancy vacancy,
        CreateVacancyPayload payload)
    {
        for (var index = 0; index < payload.ScreeningQuestions.Count; index++)
        {
            var question = payload.ScreeningQuestions[index];

            vacancy.ScreeningQuestions.Add(
                new VacancyScreeningQuestion
                {
                    Vacancy = vacancy,
                    QuestionText = question.QuestionText,
                    AnswerType = question.AnswerType,
                    RequirementType = question.RequirementType,
                    SortOrder = index
                });
        }
    }

    private static void AddFunnelStages(
        Vacancy vacancy,
        CreateVacancyPayload payload)
    {
        for (var index = 0; index < payload.FunnelStages.Count; index++)
        {
            var stage = payload.FunnelStages[index];

            vacancy.FunnelStages.Add(
                new VacancyFunnelStage
                {
                    Vacancy = vacancy,
                    StageName = stage.StageName,
                    Hours = stage.Hours,
                    IsStandard = stage.IsStandard,
                    SortOrder = index
                });
        }
    }

    private static void AddPublicationChannels(
        Vacancy vacancy,
        CreateVacancyPayload payload)
    {
        var channels = new[]
        {
            ("Core", "SkillMatch", true),
            ("Outdoor", "JobSearch.az", payload.PublishOnJobSearchAz),
            ("Outdoor", "Position.az", payload.PublishOnPositionAz),
            ("Outdoor", "Banco.az", payload.PublishOnBancoAz),
            ("Outdoor", "Busy.az", payload.PublishOnBusyAz),
            ("Social", "Twitter / X", payload.ShareOnTwitter),
            ("Social", "LinkedIn", payload.ShareOnLinkedIn)
        };

        for (var index = 0; index < channels.Length; index++)
        {
            var channel = channels[index];

            vacancy.PublicationChannels.Add(
                new VacancyPublicationChannel
                {
                    Vacancy = vacancy,
                    ChannelType = channel.Item1,
                    ChannelName = channel.Item2,
                    IsEnabled = channel.Item3,
                    SortOrder = index
                });
        }
    }
}
