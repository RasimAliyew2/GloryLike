using GloryLikeBackend.Dtos.Ai.Questionnaires;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SkillQuestionnairesController : ControllerBase
{
    private readonly ISkillQuestionnaireService _questionnaireService;

    public SkillQuestionnairesController(ISkillQuestionnaireService questionnaireService)
    {
        _questionnaireService = questionnaireService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<SkillQuestionnaireResponse>> Generate(
        [FromBody] GenerateSkillQuestionnaireRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _questionnaireService.GetOrGenerateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new
            {
                message = "Skill questionnaire generasiya/cache zamanı xəta baş verdi.",
                error = ex.Message
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Gözlənilməyən xəta baş verdi.",
                error = ex.Message
            });
        }
    }
}
