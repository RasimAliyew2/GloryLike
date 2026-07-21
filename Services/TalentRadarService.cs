using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.TalentRadar;
using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Profile;
using GloryLikeBackend.Models.Vacancies;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class TalentRadarService : ITalentRadarService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TalentRadarService> _logger;

    public TalentRadarService(
        AppDbContext dbContext,
        ILogger<TalentRadarService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TalentRadarResponse?> GetAsync(
        int employerUserId,
        CancellationToken cancellationToken = default)
    {
        var employerExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == employerUserId, cancellationToken);

        if (!employerExists)
            return null;

        var vacancies = await _dbContext.Vacancies
            .AsNoTracking()
            .Include(vacancy => vacancy.SkillRequirements)
            .Where(vacancy =>
                vacancy.EmployerUserId == employerUserId
                && vacancy.Status == "Published")
            .OrderByDescending(vacancy => vacancy.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var response = new TalentRadarResponse
        {
            Success = true,
            TotalVacancies = vacancies.Count
        };

        if (vacancies.Count == 0)
        {
            response.Message = "Talent Radar üçün employer vacancy-si tapılmadı.";
            return response;
        }

        var templates = BuildRoleTemplates(vacancies);
        response.ScoredVacancies = templates.Count;

        if (templates.Count == 0)
        {
            response.Message = "Vacancy-lərdə score hesablamaq üçün skill template tapılmadı.";
            return response;
        }

        var scoredVacancyIds = templates
            .Select(template => template.Vacancy.Id)
            .ToList();

        // Canonical Job join: skill sətrinin primary key-i istifadə edilmir.
        // UserSkills.JobFamilyId birbaşa Vacancies.JobFamilyId ilə INNER JOIN olunur.
        // Eyni account Candidate/Employer rejimləri arasında switch edə bildiyi üçün
        // employerUserId-yə bərabər UserSkills sətri də uyğun candidate sayılır.
        var candidateIds = await (
                from userSkill in _dbContext.UserSkills.AsNoTracking()
                join vacancy in _dbContext.Vacancies.AsNoTracking()
                    on userSkill.JobFamilyId equals vacancy.JobFamilyId
                where userSkill.JobFamilyId > 0
                      && scoredVacancyIds.Contains(vacancy.Id)
                select userSkill.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (candidateIds.Count == 0)
        {
            response.Message = "Vacancy JobFamilyId-lərinə uyğun candidate tapılmadı.";
            return response;
        }

        var candidateSkills = await _dbContext.UserSkills
            .AsNoTracking()
            .Where(skill => candidateIds.Contains(skill.UserId))
            .ToListAsync(cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(user => candidateIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);

        foreach (var skillGroup in candidateSkills.GroupBy(skill => skill.UserId))
        {
            if (!users.TryGetValue(skillGroup.Key, out var user))
                continue;

            var skills = skillGroup.ToList();
            var candidateJobFamilyIds = skills
                .Select(skill => skill.JobFamilyId)
                .Where(jobFamilyId => jobFamilyId > 0)
                .ToHashSet();

            var matchingTemplates = templates
                .Where(template => candidateJobFamilyIds.Contains(
                    template.Vacancy.JobFamilyId))
                .ToList();

            var scoreMap = BuildCandidateScoreMap(skills);
            var scoredRoles = new List<ScoredRole>();

            foreach (var template in matchingTemplates)
            {
                var readiness = CalculateRoleReadiness(template, scoreMap);

                if (!readiness.HasValue)
                {
                    _logger.LogWarning(
                        "Taxonomy warning: vacancy {VacancyId}/{PlatformVacancyId} üçün Role Readiness denominator-u 0-dır.",
                        template.Vacancy.Id,
                        template.Vacancy.PlatformVacancyId);
                    continue;
                }

                scoredRoles.Add(new ScoredRole
                {
                    Template = template,
                    RoleReadiness = readiness.Value
                });
            }

            var bestRole = scoredRoles
                .OrderByDescending(role => role.RoleReadiness)
                .ThenByDescending(role => role.Template.Vacancy.CreatedAtUtc)
                .FirstOrDefault();

            if (bestRole is null)
                continue;

            var matchedSkills = BuildMatchedSkills(
                bestRole.Template,
                skills,
                scoreMap);

            response.Candidates.Add(new TalentRadarCandidateDto
            {
                UserId = user.Id,
                Name = BuildCandidateName(user),
                CurrentRole = ResolveCurrentRole(
                    skills,
                    bestRole.Template.Vacancy.JobFamilyId),
                JobFamilyName = bestRole.Template.Vacancy.JobFamilyName,
                BestVacancyId = bestRole.Template.Vacancy.Id,
                PlatformVacancyId = bestRole.Template.Vacancy.PlatformVacancyId,
                TargetRoleTitle = string.IsNullOrWhiteSpace(
                    bestRole.Template.Vacancy.RoleTitle)
                    ? bestRole.Template.Vacancy.PositionName
                    : bestRole.Template.Vacancy.RoleTitle,
                RoleReadiness = bestRole.RoleReadiness,
                MatchedVacancyCount = scoredRoles.Count,
                MatchedSkillsCount = matchedSkills.Count,
                TemplateSkillsCount = bestRole.Template.Skills.Count,
                Skills = matchedSkills
            });
        }

        response.Candidates = response.Candidates
            .OrderByDescending(candidate => candidate.RoleReadiness)
            .ThenBy(candidate => candidate.Name)
            .ToList();

        response.Message = response.Candidates.Count == 0
            ? "Vacancy JobFamilyId-lərinə uyğun candidate tapılmadı."
            : $"{response.Candidates.Count} uyğun candidate tapıldı.";

        return response;
    }

    private List<RoleTemplate> BuildRoleTemplates(
        IReadOnlyCollection<Vacancy> vacancies)
    {
        var templates = new List<RoleTemplate>();

        foreach (var vacancy in vacancies)
        {
            if (vacancy.JobFamilyId <= 0)
            {
                _logger.LogWarning(
                    "Taxonomy warning: vacancy {VacancyId}/{PlatformVacancyId} üçün JobFamilyId yoxdur və Talent Radar-dan çıxarıldı.",
                    vacancy.Id,
                    vacancy.PlatformVacancyId);
                continue;
            }

            var skills = vacancy.SkillRequirements
                .Where(skill =>
                    skill.SkillId > 0
                    && !string.IsNullOrWhiteSpace(skill.SkillName))
                .GroupBy(skill => skill.SkillId)
                .Select(group =>
                {
                    var first = group
                        .OrderBy(skill => skill.SortOrder)
                        .First();

                    return new RoleTemplateSkill
                    {
                        SkillId = first.SkillId,
                        SkillName = first.SkillName.Trim(),
                        Weight = group.Max(skill => Math.Max(
                            skill.MinimumVerificationLevel,
                            0))
                    };
                })
                .ToList();

            var denominator = skills.Sum(skill => skill.Weight);
            if (skills.Count == 0 || denominator <= 0)
            {
                _logger.LogWarning(
                    "Taxonomy warning: vacancy {VacancyId}/{PlatformVacancyId} skill template-i boşdur və Talent Radar-dan çıxarıldı.",
                    vacancy.Id,
                    vacancy.PlatformVacancyId);
                continue;
            }

            templates.Add(new RoleTemplate
            {
                Vacancy = vacancy,
                Skills = skills
            });
        }

        return templates;
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
                    group => group.Max(GetSkillSignal)),
            ByName = skills
                .Where(skill => !string.IsNullOrWhiteSpace(skill.SkillName))
                .GroupBy(
                    skill => Normalize(skill.SkillName),
                    StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    group => group.Key,
                    group => group.Max(GetSkillSignal),
                    StringComparer.OrdinalIgnoreCase)
        };
    }

    private static double? CalculateRoleReadiness(
        RoleTemplate template,
        CandidateScoreMap candidateScores)
    {
        var denominator = template.Skills.Sum(skill => (double)skill.Weight);
        if (template.Skills.Count == 0 || denominator <= 0d)
            return null;

        var numerator = template.Skills.Sum(skill =>
            skill.Weight * GetCandidateScore(skill, candidateScores));

        return Math.Clamp(numerator / denominator, 0d, 100d);
    }

    private static List<TalentRadarSkillDto> BuildMatchedSkills(
        RoleTemplate template,
        IReadOnlyCollection<UserSkill> candidateSkills,
        CandidateScoreMap candidateScores)
    {
        return template.Skills
            .Select(templateSkill =>
            {
                var score = GetCandidateScore(templateSkill, candidateScores);
                var candidateSkill = FindCandidateSkill(
                    templateSkill,
                    candidateSkills);

                return new
                {
                    TemplateSkill = templateSkill,
                    CandidateSkill = candidateSkill,
                    Score = score
                };
            })
            .Where(item => item.Score > 0d)
            .OrderByDescending(item => item.TemplateSkill.Weight)
            .ThenByDescending(item => item.Score)
            .ThenBy(item => item.TemplateSkill.SkillName)
            .Select(item => new TalentRadarSkillDto
            {
                SkillId = item.TemplateSkill.SkillId,
                SkillName = item.TemplateSkill.SkillName,
                Score = RoundHalfUp(item.Score),
                IsVerified = item.CandidateSkill is not null
                    && IsVerified(item.CandidateSkill)
            })
            .ToList();
    }

    private static UserSkill? FindCandidateSkill(
        RoleTemplateSkill templateSkill,
        IReadOnlyCollection<UserSkill> candidateSkills)
    {
        return candidateSkills
            .Where(skill =>
                skill.SkillId == templateSkill.SkillId
                || Normalize(skill.SkillName).Equals(
                    Normalize(templateSkill.SkillName),
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(GetSkillSignal)
            .FirstOrDefault();
    }

    private static double GetCandidateScore(
        RoleTemplateSkill templateSkill,
        CandidateScoreMap candidateScores)
    {
        if (candidateScores.ById.TryGetValue(templateSkill.SkillId, out var byId))
            return byId;

        return candidateScores.ByName.TryGetValue(
            Normalize(templateSkill.SkillName),
            out var byName)
            ? byName
            : 0d;
    }

    private static double GetSkillSignal(UserSkill skill)
    {
        var credibility = skill.CredibilityScore > 0d
            ? Math.Clamp(skill.CredibilityScore, 0d, 100d)
            : Math.Clamp(
                (skill.KnowledgeScore * 0.45d)
                + (skill.ExperienceScore * 0.55d),
                0d,
                100d);

        if (IsVerified(skill))
            return credibility;

        var status = skill.Status?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(status)
            || status.Equals("self_declared", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Min(credibility, 40d);
        }

        return 0d;
    }

    private static bool IsVerified(UserSkill skill)
    {
        return skill.IsVerified
            || string.Equals(
                skill.Status?.Trim(),
                "verified",
                StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveCurrentRole(
        IReadOnlyCollection<UserSkill> skills,
        int jobFamilyId)
    {
        var representative = skills
            .Where(skill => skill.JobFamilyId == jobFamilyId)
            .OrderByDescending(GetSkillSignal)
            .ThenBy(skill => skill.SkillName)
            .FirstOrDefault();

        if (representative is null)
            return "Candidate";

        var role = string.Join(
            " ",
            new[]
            {
                representative.SeniorityName,
                representative.PositionName
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return string.IsNullOrWhiteSpace(role)
            ? string.IsNullOrWhiteSpace(representative.JobFamilyName)
                ? "Candidate"
                : representative.JobFamilyName.Trim()
            : role;
    }

    private static string BuildCandidateName(User user)
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
        return (int)Math.Floor(Math.Clamp(value, 0d, 100d) + 0.5d);
    }

    private static string Normalize(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }

    private sealed class RoleTemplate
    {
        public Vacancy Vacancy { get; set; } = null!;
        public List<RoleTemplateSkill> Skills { get; set; } = new();
    }

    private sealed class RoleTemplateSkill
    {
        public int SkillId { get; set; }
        public string SkillName { get; set; } = string.Empty;
        public int Weight { get; set; }
    }

    private sealed class CandidateScoreMap
    {
        public Dictionary<int, double> ById { get; set; } = new();
        public Dictionary<string, double> ByName { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ScoredRole
    {
        public RoleTemplate Template { get; set; } = null!;
        public double RoleReadiness { get; set; }
    }
}
