using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Ai.Assessments;
using GloryLikeBackend.Dtos.Ai.Questionnaires;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public class SkillDepthAssessmentService : ISkillDepthAssessmentService
{
    private readonly AppDbContext _dbContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SkillDepthAssessmentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SkillDepthAssessmentResultResponse> SubmitAsync(
        SubmitSkillDepthAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var questionnaireEntity = await _dbContext.SkillQuestionnaires
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.Id == request.QuestionnaireId &&
                x.Status == "active",
                cancellationToken);

        if (questionnaireEntity is null)
            throw new InvalidOperationException("Questionnaire tapılmadı və ya active deyil.");

        var questionnaire = JsonSerializer.Deserialize<SkillQuestionnaireResponse>(
            questionnaireEntity.StructureJson,
            JsonOptions);

        if (questionnaire is null)
            throw new InvalidOperationException("Questionnaire JSON deserialize edilə bilmədi.");

        questionnaire.QuestionnaireId = questionnaireEntity.Id;

        return CalculateScore(questionnaire, request);
    }

    private static void ValidateRequest(SubmitSkillDepthAssessmentRequest request)
    {
        if (request.QuestionnaireId == Guid.Empty)
            throw new ArgumentException("QuestionnaireId boş ola bilməz.");

        if (request.Answers.Count == 0)
            throw new ArgumentException("Ən azı 1 cavab göndərilməlidir.");

        foreach (var answer in request.Answers)
        {
            if (string.IsNullOrWhiteSpace(answer.QuestionId))
                throw new ArgumentException("Answer içində questionId boş ola bilməz.");

            if (answer.SelectedOptionIds.Count == 0)
                throw new ArgumentException($"{answer.QuestionId} üçün ən azı 1 option seçilməlidir.");
        }
    }

    private static SkillDepthAssessmentResultResponse CalculateScore(
        SkillQuestionnaireResponse questionnaire,
        SubmitSkillDepthAssessmentRequest request)
    {
        var questionById = questionnaire.Questions.ToDictionary(q => q.Id, q => q);
        var answersByQuestionId = request.Answers.ToDictionary(a => a.QuestionId, a => a);

        var revealedQuestionIds = GetRevealedQuestionIds(questionnaire, answersByQuestionId);
        var visibleQuestionIds = questionnaire.Questions
            .Where(q => !q.HiddenByDefault || revealedQuestionIds.Contains(q.Id))
            .Select(q => q.Id)
            .ToHashSet();

        var selectedOptions = new List<QuestionnaireOptionDto>();

        foreach (var answer in request.Answers)
        {
            if (!questionById.TryGetValue(answer.QuestionId, out var question))
                throw new InvalidOperationException($"Question tapılmadı: {answer.QuestionId}");

            if (!visibleQuestionIds.Contains(question.Id))
                throw new InvalidOperationException($"Hidden question trigger edilmədən cavablandırılıb: {question.Id}");

            if (question.Type.Equals("single", StringComparison.OrdinalIgnoreCase) &&
                answer.SelectedOptionIds.Count != 1)
            {
                throw new InvalidOperationException($"{question.Id} single tipidir və yalnız 1 option qəbul edir.");
            }

            foreach (var optionId in answer.SelectedOptionIds.Distinct())
            {
                var option = question.Options.FirstOrDefault(o => o.Id == optionId);

                if (option is null)
                    throw new InvalidOperationException($"{question.Id} içində option tapılmadı: {optionId}");

                selectedOptions.Add(option);
            }
        }

        var rawComplexity = selectedOptions.Sum(o => o.Weights.Complexity);
        var rawOwnership = selectedOptions.Sum(o => o.Weights.Ownership);
        var rawDepth = selectedOptions.Sum(o => o.Weights.Depth);

        var maxComplexity = Math.Max(questionnaire.Scoring.MaxComplexity, 1);
        var maxOwnership = Math.Max(questionnaire.Scoring.MaxOwnership, 1);
        var maxDepth = Math.Max(questionnaire.Scoring.MaxDepth, 1);

        var complexityRatio = Math.Clamp(rawComplexity / (double)maxComplexity, 0, 1);
        var ownershipRatio = Math.Clamp(rawOwnership / (double)maxOwnership, 0, 1);
        var depthRatio = Math.Clamp(rawDepth / (double)maxDepth, 0, 1);

        var depthScore = (int)Math.Round(
            (
                complexityRatio * 0.35 +
                ownershipRatio * 0.30 +
                depthRatio * 0.35
            ) * 100);

        depthScore = Math.Clamp(depthScore, 0, 100);

        return new SkillDepthAssessmentResultResponse
        {
            QuestionnaireId = questionnaire.QuestionnaireId,
            Skill = questionnaire.Skill,
            CompanyName = request.CompanyName,
            CompanyType = request.CompanyType,

            RawComplexity = rawComplexity,
            RawOwnership = rawOwnership,
            RawDepth = rawDepth,

            MaxComplexity = maxComplexity,
            MaxOwnership = maxOwnership,
            MaxDepth = maxDepth,

            ComplexityRatio = Math.Round(complexityRatio, 4),
            OwnershipRatio = Math.Round(ownershipRatio, 4),
            DepthRatio = Math.Round(depthRatio, 4),

            DepthScore = depthScore,

            TaskComplexity = complexityRatio < 0.40
                ? "routine"
                : complexityRatio < 0.75
                    ? "complex"
                    : "strategic",

            OwnershipLevel = ownershipRatio < 0.40
                ? "contributor"
                : ownershipRatio < 0.75
                    ? "owner"
                    : "leader",

            DepthTier = depthScore < 35
                ? "basic"
                : depthScore < 65
                    ? "proficient"
                    : depthScore < 85
                        ? "advanced"
                        : "expert",

            AnsweredQuestionCount = request.Answers.Count
        };
    }

    private static HashSet<string> GetRevealedQuestionIds(
        SkillQuestionnaireResponse questionnaire,
        Dictionary<string, SkillDepthAnswerDto> answersByQuestionId)
    {
        var revealed = new HashSet<string>();

        foreach (var question in questionnaire.Questions)
        {
            if (!answersByQuestionId.TryGetValue(question.Id, out var answer))
                continue;

            foreach (var branch in question.Branching)
            {
                if (answer.SelectedOptionIds.Contains(branch.IfOption))
                    revealed.Add(branch.RevealQuestionId);
            }
        }

        return revealed;
    }
}
