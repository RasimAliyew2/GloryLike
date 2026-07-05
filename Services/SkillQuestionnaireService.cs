using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.Ai.Questionnaires;
using GloryLikeBackend.Models.Ai;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public class SkillQuestionnaireService : ISkillQuestionnaireService
{
    private const int ActiveVersion = 1;
    private const string ActiveStatus = "active";
    private const string GeneratedByModel = "gpt-5.5";

    private readonly AppDbContext _dbContext;
    private readonly IOpenAiSkillQuestionnaireGenerator _generator;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SkillQuestionnaireService(
        AppDbContext dbContext,
        IOpenAiSkillQuestionnaireGenerator generator)
    {
        _dbContext = dbContext;
        _generator = generator;
    }

    public async Task<SkillQuestionnaireResponse> GetOrGenerateAsync(
        GenerateSkillQuestionnaireRequest request,
        CancellationToken cancellationToken = default)
    {
        NormalizeRequest(request);

        var cached = await _dbContext.SkillQuestionnaires
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.SkillName == request.Skill &&
                x.Seniority == request.Seniority &&
                x.SkillComplexity == request.SkillComplexity &&
                x.Language == request.Language &&
                x.Version == ActiveVersion &&
                x.Status == ActiveStatus,
                cancellationToken);

        if (cached is not null)
        {
            var cachedResponse = JsonSerializer.Deserialize<SkillQuestionnaireResponse>(
                cached.StructureJson,
                JsonOptions);

            if (cachedResponse is null)
                throw new InvalidOperationException("Cached questionnaire deserialize edilə bilmədi.");

            cachedResponse.QuestionnaireId = cached.Id;
            return cachedResponse;
        }

        var generated = await _generator.GenerateQuestionnaireAsync(request, cancellationToken);

        var entity = new SkillQuestionnaire
        {
            Id = Guid.NewGuid(),
            SkillName = request.Skill,
            Seniority = request.Seniority,
            SkillComplexity = request.SkillComplexity,
            Language = request.Language,
            QuestionCount = generated.Questions.Count,
            Version = ActiveVersion,
            GeneratedByModel = GeneratedByModel,
            Status = ActiveStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        generated.QuestionnaireId = entity.Id;
        entity.StructureJson = JsonSerializer.Serialize(generated, JsonOptions);

        _dbContext.SkillQuestionnaires.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return generated;
    }

    private static void NormalizeRequest(GenerateSkillQuestionnaireRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.Skill))
            throw new ArgumentException("Skill boş ola bilməz.", nameof(request.Skill));

        request.Skill = request.Skill.Trim();

        request.SkillComplexity = string.IsNullOrWhiteSpace(request.SkillComplexity)
            ? "medium"
            : request.SkillComplexity.Trim().ToLowerInvariant();

        request.Seniority = string.IsNullOrWhiteSpace(request.Seniority)
            ? "middle"
            : request.Seniority.Trim().ToLowerInvariant();

        request.Language = string.IsNullOrWhiteSpace(request.Language)
            ? "az"
            : request.Language.Trim().ToLowerInvariant();

        if (request.SkillComplexity is not ("low" or "medium" or "high"))
            throw new ArgumentException("SkillComplexity yalnız low, medium və ya high ola bilər.");

        if (request.Seniority is not ("junior" or "middle" or "senior" or "lead"))
            throw new ArgumentException("Seniority yalnız junior, middle, senior və ya lead ola bilər.");
    }
}
