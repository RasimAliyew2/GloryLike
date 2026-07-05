using GloryLikeBackend.Dtos.Ai.Questionnaires;

namespace GloryLikeBackend.Services.Interfaces;

public interface ISkillQuestionnaireService
{
    Task<SkillQuestionnaireResponse> GetOrGenerateAsync(
        GenerateSkillQuestionnaireRequest request,
        CancellationToken cancellationToken = default);
}
