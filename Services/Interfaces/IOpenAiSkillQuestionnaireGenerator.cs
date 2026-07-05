using GloryLikeBackend.Dtos.Ai.Questionnaires;

namespace GloryLikeBackend.Services.Interfaces;

public interface IOpenAiSkillQuestionnaireGenerator
{
    Task<SkillQuestionnaireResponse> GenerateQuestionnaireAsync(
        GenerateSkillQuestionnaireRequest request,
        CancellationToken cancellationToken = default);
}
