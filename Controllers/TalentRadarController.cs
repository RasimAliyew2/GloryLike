using GloryLikeBackend.Dtos.TalentRadar;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TalentRadarController : ControllerBase
{
    private readonly ITalentRadarService _talentRadarService;

    public TalentRadarController(ITalentRadarService talentRadarService)
    {
        _talentRadarService = talentRadarService;
    }

    [HttpGet("{employerUserId:int}")]
    [ProducesResponseType(typeof(TalentRadarResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(TalentRadarResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(TalentRadarResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TalentRadarResponse>> Get(
        int employerUserId,
        CancellationToken cancellationToken)
    {
        if (employerUserId <= 0)
        {
            return BadRequest(new TalentRadarResponse
            {
                Success = false,
                Message = "Employer user ID düzgün deyil."
            });
        }

        var response = await _talentRadarService.GetAsync(
            employerUserId,
            cancellationToken);

        if (response is null)
        {
            return NotFound(new TalentRadarResponse
            {
                Success = false,
                Message = "Employer user SQL-də tapılmadı."
            });
        }

        return Ok(response);
    }
}
