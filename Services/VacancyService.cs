using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Vacancies;
using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Profile;
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

    private static readonly string[] CandidateVisibleStatuses =
    {
        "Published",
        "Active"
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

    public async Task<CandidateVacancyListResponse?>
        GetCandidateVacanciesAsync(
            int candidateUserId,
            CancellationToken cancellationToken = default)
    {
        var candidateExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == candidateUserId,
                cancellationToken);

        if (!candidateExists)
            return null;

        var candidateSkills = await _dbContext.UserSkills
            .AsNoTracking()
            .Where(skill => skill.UserId == candidateUserId)
            .ToListAsync(cancellationToken);

        var jobFamilyIds = candidateSkills
            .Select(skill => skill.JobFamilyId)
            .Where(jobFamilyId => jobFamilyId > 0)
            .Distinct()
            .ToList();

        var response = new CandidateVacancyListResponse
        {
            Success = true,
            CandidateUserId = candidateUserId,
            CandidateJobFamilyIds = jobFamilyIds,
            CandidateJobFamilyNames = candidateSkills
                .Where(skill =>
                    skill.JobFamilyId > 0
                    && !string.IsNullOrWhiteSpace(skill.JobFamilyName))
                .GroupBy(skill => skill.JobFamilyId)
                .Select(group => group.First().JobFamilyName.Trim())
                .OrderBy(name => name)
                .ToList()
        };

        if (jobFamilyIds.Count == 0)
        {
            response.Message =
                "Candidate UserSkills məlumatında JobFamilyId tapılmadı.";
            return response;
        }

        var today = DateTime.UtcNow.Date;
        var vacancies = await _dbContext.Vacancies
            .AsNoTracking()
            .Include(vacancy => vacancy.SkillRequirements)
            .Include(vacancy => vacancy.Applications
                .Where(application =>
                    application.CandidateUserId == candidateUserId))
            .Where(vacancy =>
                jobFamilyIds.Contains(vacancy.JobFamilyId)
                && CandidateVisibleStatuses.Contains(vacancy.Status)
                && (!vacancy.PublishDate.HasValue
                    || vacancy.PublishDate.Value.Date <= today)
                && (!vacancy.ApplicationDeadline.HasValue
                    || vacancy.ApplicationDeadline.Value.Date >= today))
            .OrderByDescending(vacancy => vacancy.PublicationPriority)
            .ThenByDescending(vacancy => vacancy.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var employerIds = vacancies
            .Select(vacancy => vacancy.EmployerUserId)
            .Distinct()
            .ToList();

        var employers = await _dbContext.Users
            .AsNoTracking()
            .Where(user => employerIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        var candidateScoreMap = BuildCandidateScoreMap(candidateSkills);

        foreach (var vacancy in vacancies)
        {
            var templateSkills = BuildCandidateVacancyTemplate(
                vacancy,
                candidateScoreMap);
            var application = vacancy.Applications.SingleOrDefault();

            response.Vacancies.Add(new CandidateVacancyListItemDto
            {
                VacancyId = vacancy.Id,
                PlatformVacancyId = vacancy.PlatformVacancyId,
                EmployerUserId = vacancy.EmployerUserId,
                EmployerName = employers.TryGetValue(
                    vacancy.EmployerUserId,
                    out var employer)
                    ? BuildUserDisplayName(employer)
                    : $"Employer #{vacancy.EmployerUserId}",
                JobFamilyId = vacancy.JobFamilyId,
                JobFamilyName = vacancy.JobFamilyName,
                RoleTitle = vacancy.RoleTitle,
                PositionName = vacancy.PositionName,
                SeniorityName = vacancy.SeniorityName,
                EmploymentType = vacancy.EmploymentType,
                MinSalary = vacancy.MinSalary,
                MaxSalary = vacancy.MaxSalary,
                Currency = vacancy.Currency,
                HideSalary = vacancy.HideSalary,
                JobDescription = vacancy.JobDescription,
                Visibility = vacancy.Visibility,
                PublishDate = vacancy.PublishDate,
                ApplicationDeadline = vacancy.ApplicationDeadline,
                CreatedAtUtc = vacancy.CreatedAtUtc,
                MatchScore = CalculateCandidateVacancyReadiness(
                    templateSkills,
                    candidateScoreMap),
                Skills = templateSkills,
                HasApplied = application is not null,
                ApplicationId = application?.Id,
                ApplicationStatus = application?.Status ?? string.Empty,
                AppliedAtUtc = application?.AppliedAtUtc
            });
        }

        response.Message = response.Vacancies.Count == 0
            ? "Candidate JobFamilyId-lərinə uyğun aktiv vacancy tapılmadı."
            : $"{response.Vacancies.Count} uyğun vacancy tapıldı.";

        return response;
    }

    public async Task<List<EmployerVacancyListItemDto>>
        GetEmployerVacanciesAsync(
            int employerUserId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vacancies
            .AsNoTracking()
            .Where(vacancy => vacancy.EmployerUserId == employerUserId)
            .OrderByDescending(vacancy => vacancy.CreatedAtUtc)
            .Select(vacancy => new EmployerVacancyListItemDto
            {
                VacancyId = vacancy.Id,
                PlatformVacancyId = vacancy.PlatformVacancyId,
                RoleTitle = vacancy.RoleTitle,
                JobFamilyName = vacancy.JobFamilyName,
                PositionName = vacancy.PositionName,
                Status = vacancy.Status,

                CandidateCount = vacancy.Applications.Count,
                PublishDate = vacancy.PublishDate,
                ApplicationDeadline = vacancy.ApplicationDeadline,
                CreatedAtUtc = vacancy.CreatedAtUtc,
                UpdatedAtUtc = vacancy.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<EmployerVacancyDetailResponse?>
        GetEmployerVacancyDetailAsync(
            int employerUserId,
            int vacancyId,
            CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0 || vacancyId <= 0)
            return null;

        var vacancy = await _dbContext.Vacancies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.SkillRequirements)
            .Include(item => item.FunnelStages)
            .Include(item => item.Applications)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == vacancyId
                    && item.EmployerUserId == employerUserId,
                cancellationToken);

        if (vacancy is null)
            return null;

        var candidateIds = vacancy.Applications
            .Select(application => application.CandidateUserId)
            .Distinct()
            .ToList();

        var usersById = candidateIds.Count == 0
            ? new Dictionary<int, User>()
            : await _dbContext.Users
                .AsNoTracking()
                .Where(user => candidateIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, cancellationToken);

        var candidateSkills = candidateIds.Count == 0
            ? new List<UserSkill>()
            : await _dbContext.UserSkills
                .AsNoTracking()
                .Where(skill => candidateIds.Contains(skill.UserId))
                .ToListAsync(cancellationToken);

        var skillsByCandidate = candidateSkills
            .GroupBy(skill => skill.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        var applicants = new List<EmployerVacancyApplicantDto>();

        foreach (var application in vacancy.Applications)
        {
            var skills = skillsByCandidate.TryGetValue(
                application.CandidateUserId,
                out var savedSkills)
                ? savedSkills
                : new List<UserSkill>();
            var candidateScoreMap = BuildCandidateScoreMap(skills);
            var templateSkills = BuildCandidateVacancyTemplate(
                vacancy,
                candidateScoreMap);
            var score = CalculateCandidateVacancyReadiness(
                templateSkills,
                candidateScoreMap);

            applicants.Add(new EmployerVacancyApplicantDto
            {
                ApplicationId = application.Id,
                CandidateUserId = application.CandidateUserId,
                CandidateName = usersById.TryGetValue(
                    application.CandidateUserId,
                    out var candidate)
                    ? BuildCandidateDisplayName(candidate)
                    : $"Candidate #{application.CandidateUserId}",
                CurrentRole = ResolveCandidateRole(
                    skills,
                    vacancy.JobFamilyId,
                    vacancy.JobFamilyName),
                MatchScore = score,
                ApplicationStatus = application.Status,
                AppliedAtUtc = application.AppliedAtUtc,
                MatchedSkills = templateSkills
                    .Where(skill => skill.IsMatched)
                    .Select(skill => skill.SkillName)
                    .ToList(),
                MissingSkills = templateSkills
                    .Where(skill => !skill.IsMatched)
                    .Select(skill => skill.SkillName)
                    .ToList()
            });
        }

        applicants = applicants
            .OrderByDescending(applicant => applicant.MatchScore)
            .ThenBy(applicant => applicant.AppliedAtUtc)
            .ThenBy(applicant => applicant.CandidateName)
            .ToList();

        var detail = new EmployerVacancyDetailDto
        {
            VacancyId = vacancy.Id,
            PlatformVacancyId = vacancy.PlatformVacancyId,
            EmployerUserId = vacancy.EmployerUserId,
            JobFamilyId = vacancy.JobFamilyId,
            JobFamilyName = vacancy.JobFamilyName,
            SeniorityName = vacancy.SeniorityName,
            PositionName = vacancy.PositionName,
            RoleTitle = vacancy.RoleTitle,
            EmploymentType = vacancy.EmploymentType,
            JobDescription = vacancy.JobDescription,
            Visibility = vacancy.Visibility,
            Status = vacancy.Status,
            PublishDate = vacancy.PublishDate,
            ApplicationDeadline = vacancy.ApplicationDeadline,
            CreatedAtUtc = vacancy.CreatedAtUtc,
            UpdatedAtUtc = vacancy.UpdatedAtUtc,
            ApplicantCount = applicants.Count,
            AverageMatchScore = applicants.Count == 0
                ? 0
                : RoundHalfUp(applicants.Average(item => item.MatchScore)),
            HighConfidenceCount = applicants.Count(
                applicant => applicant.MatchScore >= 60),
            BestMatch = applicants.FirstOrDefault(),
            Applicants = applicants,
            Skills = vacancy.SkillRequirements
                .OrderBy(skill => skill.SortOrder)
                .Select(skill => new EmployerVacancySkillDto
                {
                    SkillId = skill.SkillId,
                    SkillName = skill.SkillName,
                    Weight = skill.MinimumVerificationLevel,
                    RequirementType = skill.RequirementType
                })
                .ToList(),
            FunnelStages = vacancy.FunnelStages
                .OrderBy(stage => stage.SortOrder)
                .Select(stage => new EmployerVacancyFunnelStageDto
                {
                    StageName = stage.StageName,
                    Hours = stage.Hours,
                    IsStandard = stage.IsStandard,
                    SortOrder = stage.SortOrder
                })
                .ToList()
        };

        return new EmployerVacancyDetailResponse
        {
            Success = true,
            Message = applicants.Count == 0
                ? "Vacancy tapıldı, hələ müraciət edən candidate yoxdur."
                : $"Vacancy və {applicants.Count} müraciət yükləndi.",
            EmployerUserId = employerUserId,
            Vacancy = detail
        };
    }

    public async Task<EmployerVacancyEditResponse?>
        GetEmployerVacancyForEditAsync(
            int employerUserId,
            int vacancyId,
            CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0 || vacancyId <= 0)
            return null;

        var vacancy = await _dbContext.Vacancies
            .AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.SkillRequirements)
            .Include(item => item.Benefits)
            .Include(item => item.ApplicationRequirements)
            .Include(item => item.ScreeningQuestions)
            .Include(item => item.FunnelStages)
            .Include(item => item.PublicationChannels)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == vacancyId
                    && item.EmployerUserId == employerUserId,
                cancellationToken);

        if (vacancy is null)
            return null;

        return new EmployerVacancyEditResponse
        {
            Success = true,
            Message = "Vacancy edit məlumatları SQL-dən yükləndi.",
            EmployerUserId = employerUserId,
            VacancyId = vacancy.Id,
            Status = vacancy.Status,
            Vacancy = MapVacancyToEditPayload(vacancy)
        };
    }

    public async Task<ToggleEmployerVacancyStatusResult>
        ToggleEmployerStatusAsync(
            int employerUserId,
            int vacancyId,
            CancellationToken cancellationToken = default)
    {
        if (employerUserId <= 0 || vacancyId <= 0)
        {
            return ToggleEmployerVacancyStatusResult.Invalid(
                employerUserId,
                vacancyId,
                "Employer və vacancy ID düzgün olmalıdır.");
        }

        var vacancy = await _dbContext.Vacancies
            .FirstOrDefaultAsync(
                item =>
                    item.Id == vacancyId
                    && item.EmployerUserId == employerUserId,
                cancellationToken);

        if (vacancy is null)
        {
            return ToggleEmployerVacancyStatusResult.NotFound(
                employerUserId,
                vacancyId,
                "Vacancy tapılmadı və ya bu employer-ə aid deyil.");
        }

        var normalizedStatus = vacancy.Status.Trim();

        if (normalizedStatus.Equals(
                "Suspended",
                StringComparison.OrdinalIgnoreCase)
            || normalizedStatus.Equals(
                "Paused",
                StringComparison.OrdinalIgnoreCase))
        {
            vacancy.Status = "Active";
        }
        else if (normalizedStatus.Equals(
                     "Published",
                     StringComparison.OrdinalIgnoreCase)
                 || normalizedStatus.Equals(
                     "Active",
                     StringComparison.OrdinalIgnoreCase))
        {
            vacancy.Status = "Suspended";
        }
        else
        {
            return ToggleEmployerVacancyStatusResult.Invalid(
                employerUserId,
                vacancyId,
                $"{vacancy.Status} statuslu vacancy dayandırıla və ya davam etdirilə bilməz.");
        }

        vacancy.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToggleEmployerVacancyStatusResult.Updated(vacancy);
    }

    public async Task<ApplyToVacancyResult> ApplyToVacancyAsync(
        int vacancyId,
        int candidateUserId,
        CancellationToken cancellationToken = default)
    {
        if (vacancyId <= 0 || candidateUserId <= 0)
        {
            return ApplyToVacancyResult.Invalid(
                vacancyId,
                candidateUserId,
                "Vacancy və candidate user ID düzgün olmalıdır.");
        }

        var candidateExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == candidateUserId,
                cancellationToken);

        if (!candidateExists)
        {
            return ApplyToVacancyResult.NotFound(
                vacancyId,
                candidateUserId,
                "Candidate user SQL-də tapılmadı.");
        }

        var vacancy = await _dbContext.Vacancies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == vacancyId,
                cancellationToken);

        if (vacancy is null)
        {
            return ApplyToVacancyResult.NotFound(
                vacancyId,
                candidateUserId,
                "Vacancy SQL-də tapılmadı.");
        }

        var today = DateTime.UtcNow.Date;
        var isVisible = CandidateVisibleStatuses.Contains(
            vacancy.Status,
            StringComparer.OrdinalIgnoreCase);

        if (!isVisible
            || (vacancy.PublishDate.HasValue
                && vacancy.PublishDate.Value.Date > today)
            || (vacancy.ApplicationDeadline.HasValue
                && vacancy.ApplicationDeadline.Value.Date < today))
        {
            return ApplyToVacancyResult.Invalid(
                vacancyId,
                candidateUserId,
                "Bu vacancy hazırda müraciət üçün aktiv deyil.");
        }

        var hasMatchingJob = await _dbContext.UserSkills
            .AsNoTracking()
            .AnyAsync(
                skill =>
                    skill.UserId == candidateUserId
                    && skill.JobFamilyId > 0
                    && skill.JobFamilyId == vacancy.JobFamilyId,
                cancellationToken);

        if (!hasMatchingJob)
        {
            return ApplyToVacancyResult.Invalid(
                vacancyId,
                candidateUserId,
                "Candidate JobFamilyId-si bu vacancy ilə uyğun deyil.");
        }

        var existing = await _dbContext.VacancyApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(
                application =>
                    application.VacancyId == vacancyId
                    && application.CandidateUserId == candidateUserId,
                cancellationToken);

        if (existing is not null)
            return ApplyToVacancyResult.Applied(existing, true);

        var now = DateTime.UtcNow;
        var application = new VacancyApplication
        {
            VacancyId = vacancyId,
            CandidateUserId = candidateUserId,
            Status = VacancyApplicationStatuses.NoResponseYet,
            AppliedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.VacancyApplications.Add(application);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ApplyToVacancyResult.Applied(application, false);
        }
        catch (DbUpdateException exception)
        {
            _dbContext.Entry(application).State = EntityState.Detached;

            existing = await _dbContext.VacancyApplications
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    item =>
                        item.VacancyId == vacancyId
                        && item.CandidateUserId == candidateUserId,
                    cancellationToken);

            if (existing is not null)
                return ApplyToVacancyResult.Applied(existing, true);

            _logger.LogError(
                exception,
                "Candidate {CandidateUserId} vacancy {VacancyId} üçün apply edilərkən SQL xətası baş verdi.",
                candidateUserId,
                vacancyId);

            throw;
        }
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

    public async Task<UpdateVacancyResult> UpdateAsync(
        int vacancyId,
        CreateVacancyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (vacancyId <= 0 || request.EmployerUserId <= 0)
        {
            return UpdateVacancyResult.Invalid(
                request.EmployerUserId,
                vacancyId,
                "Employer və vacancy ID düzgün olmalıdır.");
        }

        var payload = request.Vacancy ?? new CreateVacancyPayload();
        Normalize(payload);

        var payloadValidationMessage = ValidatePayload(payload);

        if (!string.IsNullOrWhiteSpace(payloadValidationMessage))
        {
            return UpdateVacancyResult.Invalid(
                request.EmployerUserId,
                vacancyId,
                payloadValidationMessage);
        }

        var vacancy = await _dbContext.Vacancies
            .AsSplitQuery()
            .Include(item => item.SkillRequirements)
            .Include(item => item.Benefits)
            .Include(item => item.ApplicationRequirements)
            .Include(item => item.ScreeningQuestions)
            .Include(item => item.FunnelStages)
            .Include(item => item.PublicationChannels)
            .FirstOrDefaultAsync(
                item =>
                    item.Id == vacancyId
                    && item.EmployerUserId == request.EmployerUserId,
                cancellationToken);

        if (vacancy is null)
        {
            return UpdateVacancyResult.NotFound(
                request.EmployerUserId,
                vacancyId,
                "Vacancy tapılmadı və ya bu employer-ə aid deyil.");
        }

        if (!payload.PlatformVacancyId.Equals(
                vacancy.PlatformVacancyId,
                StringComparison.Ordinal))
        {
            return UpdateVacancyResult.Invalid(
                request.EmployerUserId,
                vacancyId,
                "Platform Vacancy ID dəyişdirilə bilməz.");
        }

        var jobFamily = await _dbContext.JobFamilies
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == payload.JobFamilyId,
                cancellationToken);

        if (jobFamily is null)
        {
            return UpdateVacancyResult.Invalid(
                request.EmployerUserId,
                vacancyId,
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
            return UpdateVacancyResult.Invalid(
                request.EmployerUserId,
                vacancyId,
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
            return UpdateVacancyResult.Invalid(
                request.EmployerUserId,
                vacancyId,
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
            return UpdateVacancyResult.Invalid(
                request.EmployerUserId,
                vacancyId,
                $"SkillId {missingSkillId} SQL taxonomy-də tapılmadı.");
        }

        ApplyEditableValues(
            vacancy,
            payload,
            jobFamily,
            seniority,
            position);

        ReplaceEditableCollections(
            vacancy,
            payload,
            skillsById);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Vacancy {VacancyId}/{PlatformVacancyId} SQL-də update edilmədi.",
                vacancy.Id,
                vacancy.PlatformVacancyId);

            throw;
        }

        return UpdateVacancyResult.Updated(vacancy);
    }

    private static CandidateScoreMap BuildCandidateScoreMap(
        IReadOnlyCollection<UserSkill> skills)
    {
        return new CandidateScoreMap
        {
            ById = skills
                .Where(skill => skill.SkillId > 0)
                .GroupBy(skill => skill.SkillId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(GetCandidateSkillSignal)),
            ByName = skills
                .Where(skill =>
                    !string.IsNullOrWhiteSpace(skill.SkillName))
                .GroupBy(
                    skill => NormalizeSkillName(skill.SkillName),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(GetCandidateSkillSignal),
                    StringComparer.OrdinalIgnoreCase)
        };
    }

    private static List<CandidateVacancySkillDto>
        BuildCandidateVacancyTemplate(
            Vacancy vacancy,
            CandidateScoreMap candidateScores)
    {
        return vacancy.SkillRequirements
            .Where(skill =>
                skill.SkillId > 0
                && !string.IsNullOrWhiteSpace(skill.SkillName))
            .GroupBy(skill => skill.SkillId)
            .Select(group =>
            {
                var first = group
                    .OrderBy(skill => skill.SortOrder)
                    .First();

                var templateSkill = new CandidateVacancySkillDto
                {
                    SkillId = first.SkillId,
                    SkillName = first.SkillName.Trim(),
                    Weight = group.Max(skill => Math.Max(
                        skill.MinimumVerificationLevel,
                        0)),
                    RequirementType = first.RequirementType
                };

                templateSkill.IsMatched = GetCandidateScore(
                    templateSkill,
                    candidateScores) > 0d;

                return templateSkill;
            })
            .OrderByDescending(skill => skill.Weight)
            .ThenBy(skill => skill.SkillName)
            .ToList();
    }

    private static int CalculateCandidateVacancyReadiness(
        IReadOnlyCollection<CandidateVacancySkillDto> templateSkills,
        CandidateScoreMap candidateScores)
    {
        var denominator = templateSkills.Sum(
            skill => (double)skill.Weight);

        if (templateSkills.Count == 0 || denominator <= 0d)
            return 0;

        var numerator = templateSkills.Sum(skill =>
            skill.Weight * GetCandidateScore(skill, candidateScores));

        return (int)Math.Floor(
            Math.Clamp(numerator / denominator, 0d, 100d) + 0.5d);
    }

    private static double GetCandidateScore(
        CandidateVacancySkillDto templateSkill,
        CandidateScoreMap candidateScores)
    {
        if (candidateScores.ById.TryGetValue(
            templateSkill.SkillId,
            out var byId))
        {
            return byId;
        }

        return candidateScores.ByName.TryGetValue(
            NormalizeSkillName(templateSkill.SkillName),
            out var byName)
            ? byName
            : 0d;
    }

    private static double GetCandidateSkillSignal(UserSkill skill)
    {
        var credibility = skill.CredibilityScore > 0d
            ? Math.Clamp(skill.CredibilityScore, 0d, 100d)
            : Math.Clamp(
                (skill.KnowledgeScore * 0.45d)
                + (skill.ExperienceScore * 0.55d),
                0d,
                100d);

        if (skill.IsVerified
            || string.Equals(
                skill.Status?.Trim(),
                "verified",
                StringComparison.OrdinalIgnoreCase))
        {
            return credibility;
        }

        var status = skill.Status?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(status)
            || status.Equals(
                "self_declared",
                StringComparison.OrdinalIgnoreCase))
        {
            return Math.Min(credibility, 40d);
        }

        return 0d;
    }

    private static string BuildUserDisplayName(User user)
    {
        var fullName = string.Join(
            " ",
            new[] { user.Name, user.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(fullName)
            ? string.IsNullOrWhiteSpace(user.UserName)
                ? $"Employer #{user.Id}"
                : user.UserName.Trim()
            : fullName;
    }

    private static string ResolveCandidateRole(
        IReadOnlyCollection<UserSkill> skills,
        int jobFamilyId,
        string fallbackJobFamilyName)
    {
        var representative = skills
            .Where(skill => skill.JobFamilyId == jobFamilyId)
            .OrderByDescending(GetCandidateSkillSignal)
            .ThenBy(skill => skill.SkillName)
            .FirstOrDefault();

        if (representative is null)
        {
            return string.IsNullOrWhiteSpace(fallbackJobFamilyName)
                ? "Candidate"
                : fallbackJobFamilyName.Trim();
        }

        var role = string.Join(
            " · ",
            new[]
            {
                representative.SeniorityName,
                representative.PositionName
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        if (!string.IsNullOrWhiteSpace(role))
            return role;

        return string.IsNullOrWhiteSpace(representative.JobFamilyName)
            ? "Candidate"
            : representative.JobFamilyName.Trim();
    }

    private static string BuildCandidateDisplayName(User user)
    {
        var fullName = string.Join(
            " ",
            new[] { user.Name, user.Surname }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(fullName)
            ? string.IsNullOrWhiteSpace(user.UserName)
                ? $"Candidate #{user.Id}"
                : user.UserName.Trim()
            : fullName;
    }

    private static int RoundHalfUp(double value)
    {
        return (int)Math.Floor(
            Math.Clamp(value, 0d, 100d) + 0.5d);
    }

    private static string NormalizeSkillName(string value)
    {
        return (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
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

    private static CreateVacancyPayload MapVacancyToEditPayload(
        Vacancy vacancy)
    {
        var standardRequirements = vacancy.ApplicationRequirements
            .Where(item => !item.IsCustom)
            .ToDictionary(
                item => item.FieldKey,
                item => item,
                StringComparer.OrdinalIgnoreCase);

        var channels = vacancy.PublicationChannels
            .ToDictionary(
                item => item.ChannelName,
                item => item.IsEnabled,
                StringComparer.OrdinalIgnoreCase);

        return new CreateVacancyPayload
        {
            JobFamilyId = vacancy.JobFamilyId,
            SeniorityId = vacancy.SeniorityId,
            PositionId = vacancy.PositionId,
            RoleTitle = vacancy.RoleTitle,
            PlatformVacancyId = vacancy.PlatformVacancyId,
            ClientRequisitionCode = vacancy.ClientRequisitionCode,
            EmploymentType = vacancy.EmploymentType,
            ExperienceRequired = vacancy.ExperienceRequired,
            EducationRequirement = vacancy.EducationRequirement,
            EducationLevel = vacancy.EducationLevel,
            MinSalary = vacancy.MinSalary,
            MaxSalary = vacancy.MaxSalary,
            PaymentTerms = vacancy.PaymentTerms,
            Currency = vacancy.Currency,
            HideSalary = vacancy.HideSalary,
            JobDescription = vacancy.JobDescription,
            SkillRequirements = vacancy.SkillRequirements
                .OrderBy(item => item.SortOrder)
                .Select(item => new CreateVacancySkillRequirementRequest
                {
                    SkillId = item.SkillId,
                    MinimumVerificationLevel =
                        item.MinimumVerificationLevel,
                    RequirementType = item.RequirementType
                })
                .ToList(),
            SelectedSkillIds = vacancy.SkillRequirements
                .OrderBy(item => item.SortOrder)
                .Select(item => item.SkillId)
                .ToList(),
            MinimumVerificationLevel = vacancy.MinimumVerificationLevel,
            Benefits = vacancy.Benefits
                .OrderBy(item => item.SortOrder)
                .Select(item => item.Name)
                .ToList(),
            ApplicationRequirements = new
                CreateVacancyApplicationRequirementsRequest
                {
                    FullName = GetRequirementMode(
                        standardRequirements,
                        "fullName",
                        ApplicationRequirementModeRequest.Required),
                    Email = GetRequirementMode(
                        standardRequirements,
                        "email",
                        ApplicationRequirementModeRequest.Required),
                    Phone = GetRequirementMode(
                        standardRequirements,
                        "phone",
                        ApplicationRequirementModeRequest.Optional),
                    Location = GetRequirementMode(
                        standardRequirements,
                        "location",
                        ApplicationRequirementModeRequest.Optional),
                    WorkExperience = GetRequirementMode(
                        standardRequirements,
                        "workExperience",
                        ApplicationRequirementModeRequest.Required),
                    CurrentPosition = GetRequirementMode(
                        standardRequirements,
                        "currentPosition",
                        ApplicationRequirementModeRequest.Optional),
                    PreviousCompanies = GetRequirementMode(
                        standardRequirements,
                        "previousCompanies",
                        ApplicationRequirementModeRequest.Optional),
                    Education = GetRequirementMode(
                        standardRequirements,
                        "education",
                        ApplicationRequirementModeRequest.Optional),
                    Certifications = GetRequirementMode(
                        standardRequirements,
                        "certifications",
                        ApplicationRequirementModeRequest.Optional),
                    Trainings = GetRequirementMode(
                        standardRequirements,
                        "trainings",
                        ApplicationRequirementModeRequest.Hidden),
                    Languages = GetRequirementMode(
                        standardRequirements,
                        "languages",
                        ApplicationRequirementModeRequest.Optional),
                    Tools = GetRequirementMode(
                        standardRequirements,
                        "tools",
                        ApplicationRequirementModeRequest.Hidden),
                    LinkedIn = GetRequirementMode(
                        standardRequirements,
                        "linkedIn",
                        ApplicationRequirementModeRequest.Optional),
                    GitHub = GetRequirementMode(
                        standardRequirements,
                        "gitHub",
                        ApplicationRequirementModeRequest.Hidden),
                    Portfolio = GetRequirementMode(
                        standardRequirements,
                        "portfolio",
                        ApplicationRequirementModeRequest.Hidden),
                    PersonalWebsite = GetRequirementMode(
                        standardRequirements,
                        "personalWebsite",
                        ApplicationRequirementModeRequest.Hidden),
                    CoverLetter = GetRequirementMode(
                        standardRequirements,
                        "coverLetter",
                        ApplicationRequirementModeRequest.Optional),
                    AdditionalFiles = GetRequirementMode(
                        standardRequirements,
                        "additionalFiles",
                        ApplicationRequirementModeRequest.Hidden),
                    CustomFields = vacancy.ApplicationRequirements
                        .Where(item => item.IsCustom)
                        .OrderBy(item => item.SortOrder)
                        .Select(item => new CreateVacancyCustomFieldRequest
                        {
                            Label = item.Label,
                            Requirement = ParseRequirementMode(
                                item.RequirementMode,
                                ApplicationRequirementModeRequest.Optional)
                        })
                        .ToList()
                },
            ScreeningQuestions = vacancy.ScreeningQuestions
                .OrderBy(item => item.SortOrder)
                .Select(item => new CreateVacancyScreeningQuestionRequest
                {
                    QuestionText = item.QuestionText,
                    AnswerType = item.AnswerType,
                    RequirementType = item.RequirementType
                })
                .ToList(),
            MinimumMatchScore = vacancy.MinimumMatchScore,
            MinimumTrustScore = vacancy.MinimumTrustScore,
            AutoRejectBelowScore = vacancy.AutoRejectBelowScore,
            RequireVerifiedCoreSkills = vacancy.RequireVerifiedCoreSkills,
            ScreeningNotes = vacancy.ScreeningNotes,
            FunnelStages = vacancy.FunnelStages
                .OrderBy(item => item.SortOrder)
                .Select(item => new CreateVacancyFunnelStageRequest
                {
                    StageName = item.StageName,
                    Hours = item.Hours,
                    IsStandard = item.IsStandard
                })
                .ToList(),
            StageApplied = vacancy.StageApplied,
            StageScreening = vacancy.StageScreening,
            StageInterview = vacancy.StageInterview,
            StageOffer = vacancy.StageOffer,
            InterviewRounds = vacancy.InterviewRounds,
            ScreeningSlaDays = vacancy.ScreeningSlaDays,
            Visibility = vacancy.Visibility,
            PublishDate = vacancy.PublishDate,
            ApplicationDeadline = vacancy.ApplicationDeadline,
            ContactEmail = vacancy.ContactEmail,
            AllowInternalCandidates = vacancy.AllowInternalCandidates,
            NotifyMatchingCandidates = vacancy.NotifyMatchingCandidates,
            PublishOnSkillMatch = GetChannelEnabled(
                channels,
                "SkillMatch",
                true),
            PublishOnJobSearchAz = GetChannelEnabled(
                channels,
                "JobSearch.az"),
            PublishOnPositionAz = GetChannelEnabled(
                channels,
                "Position.az"),
            PublishOnBancoAz = GetChannelEnabled(
                channels,
                "Banco.az"),
            PublishOnBusyAz = GetChannelEnabled(
                channels,
                "Busy.az"),
            ShareOnTwitter = GetChannelEnabled(
                channels,
                "Twitter / X"),
            ShareOnLinkedIn = GetChannelEnabled(
                channels,
                "LinkedIn"),
            PublicationPriority = vacancy.PublicationPriority
        };
    }

    private static ApplicationRequirementModeRequest GetRequirementMode(
        IReadOnlyDictionary<string, VacancyApplicationRequirement> fields,
        string fieldKey,
        ApplicationRequirementModeRequest fallback)
    {
        return fields.TryGetValue(fieldKey, out var field)
            ? ParseRequirementMode(field.RequirementMode, fallback)
            : fallback;
    }

    private static ApplicationRequirementModeRequest ParseRequirementMode(
        string? value,
        ApplicationRequirementModeRequest fallback)
    {
        return Enum.TryParse<ApplicationRequirementModeRequest>(
            value,
            true,
            out var parsed)
            && Enum.IsDefined(parsed)
                ? parsed
                : fallback;
    }

    private static bool GetChannelEnabled(
        IReadOnlyDictionary<string, bool> channels,
        string channelName,
        bool fallback = false)
    {
        return channels.TryGetValue(channelName, out var enabled)
            ? enabled
            : fallback;
    }

    private static void ApplyEditableValues(
        Vacancy vacancy,
        CreateVacancyPayload payload,
        JobFamily jobFamily,
        Seniority seniority,
        Position position)
    {
        vacancy.JobFamilyId = payload.JobFamilyId;
        vacancy.SeniorityId = payload.SeniorityId;
        vacancy.PositionId = payload.PositionId;
        vacancy.JobFamilyName = jobFamily.JobName.Trim();
        vacancy.SeniorityName = seniority.Name.Trim();
        vacancy.PositionName = position.Name.Trim();
        vacancy.RoleTitle = payload.RoleTitle;
        vacancy.ClientRequisitionCode = payload.ClientRequisitionCode;
        vacancy.EmploymentType = payload.EmploymentType;
        vacancy.ExperienceRequired = payload.ExperienceRequired;
        vacancy.EducationRequirement = payload.EducationRequirement;
        vacancy.EducationLevel = payload.EducationLevel;
        vacancy.MinSalary = payload.MinSalary;
        vacancy.MaxSalary = payload.MaxSalary;
        vacancy.PaymentTerms = payload.PaymentTerms;
        vacancy.Currency = payload.Currency;
        vacancy.HideSalary = payload.HideSalary;
        vacancy.JobDescription = payload.JobDescription;
        vacancy.MinimumVerificationLevel =
            payload.MinimumVerificationLevel;
        vacancy.MinimumMatchScore = payload.MinimumMatchScore;
        vacancy.MinimumTrustScore = payload.MinimumTrustScore;
        vacancy.AutoRejectBelowScore = payload.AutoRejectBelowScore;
        vacancy.RequireVerifiedCoreSkills =
            payload.RequireVerifiedCoreSkills;
        vacancy.ScreeningNotes = payload.ScreeningNotes;
        vacancy.StageApplied = payload.StageApplied;
        vacancy.StageScreening = payload.StageScreening;
        vacancy.StageInterview = payload.StageInterview;
        vacancy.StageOffer = payload.StageOffer;
        vacancy.InterviewRounds = payload.InterviewRounds;
        vacancy.ScreeningSlaDays = payload.ScreeningSlaDays;
        vacancy.Visibility = payload.Visibility;
        vacancy.PublishDate = payload.PublishDate;
        vacancy.ApplicationDeadline = payload.ApplicationDeadline;
        vacancy.ContactEmail = payload.ContactEmail;
        vacancy.AllowInternalCandidates = payload.AllowInternalCandidates;
        vacancy.NotifyMatchingCandidates =
            payload.NotifyMatchingCandidates;
        vacancy.PublicationPriority = payload.PublicationPriority;
        vacancy.SourcePayloadJson = JsonSerializer.Serialize(
            payload,
            PayloadJsonOptions);
        vacancy.UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ReplaceEditableCollections(
        Vacancy vacancy,
        CreateVacancyPayload payload,
        IReadOnlyDictionary<int, Skill> skillsById)
    {
        _dbContext.VacancySkillRequirements.RemoveRange(
            vacancy.SkillRequirements);
        _dbContext.VacancyBenefits.RemoveRange(vacancy.Benefits);
        _dbContext.VacancyApplicationRequirements.RemoveRange(
            vacancy.ApplicationRequirements);
        _dbContext.VacancyScreeningQuestions.RemoveRange(
            vacancy.ScreeningQuestions);
        _dbContext.VacancyFunnelStages.RemoveRange(vacancy.FunnelStages);
        _dbContext.VacancyPublicationChannels.RemoveRange(
            vacancy.PublicationChannels);

        vacancy.SkillRequirements = new List<VacancySkillRequirement>();
        vacancy.Benefits = new List<VacancyBenefit>();
        vacancy.ApplicationRequirements =
            new List<VacancyApplicationRequirement>();
        vacancy.ScreeningQuestions =
            new List<VacancyScreeningQuestion>();
        vacancy.FunnelStages = new List<VacancyFunnelStage>();
        vacancy.PublicationChannels =
            new List<VacancyPublicationChannel>();

        AddSkillRequirements(vacancy, payload, skillsById);
        AddBenefits(vacancy, payload);
        AddApplicationRequirements(vacancy, payload);
        AddScreeningQuestions(vacancy, payload);
        AddFunnelStages(vacancy, payload);
        AddPublicationChannels(vacancy, payload);
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

    private sealed class CandidateScoreMap
    {
        public Dictionary<int, double> ById { get; set; } = new();

        public Dictionary<string, double> ByName { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
