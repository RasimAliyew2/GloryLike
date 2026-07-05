using GloryLikeBackend.Dtos.Ai.Assessments;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillDepthAssessmentsController : ControllerBase
{
    private readonly ISkillDepthAssessmentService _assessmentService;

    public SkillDepthAssessmentsController(ISkillDepthAssessmentService assessmentService)
    {
        _assessmentService = assessmentService;
    }

    [HttpPost("submit")]
    public async Task<ActionResult<SkillDepthAssessmentResultResponse>> Submit(
        [FromBody] SubmitSkillDepthAssessmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _assessmentService.SubmitAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Skill assessment submit zamanı xəta baş verdi.",
                error = ex.Message
            });
        }
    }
}
