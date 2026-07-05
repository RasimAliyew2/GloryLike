using GloryLikeBackend.Dtos.Ai.Assessments;

namespace GloryLikeBackend.Services.Interfaces;

public interface ISkillDepthAssessmentService
{
    Task<SkillDepthAssessmentResultResponse> SubmitAsync(
        SubmitSkillDepthAssessmentRequest request,
        CancellationToken cancellationToken = default);
}
