using GloryLikeBackend.Dtos.EmployerCandidates;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/employer/candidates")]
public sealed class EmployerCandidatesController : ControllerBase
{
    private readonly IEmployerCandidateMessagingService _service;

    public EmployerCandidatesController(
        IEmployerCandidateMessagingService service)
    {
        _service = service;
    }

    [HttpGet("{candidateUserId:int}")]
    public async Task<ActionResult<EmployerCandidateProfileResponse>> GetProfile(
        int candidateUserId,
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetCandidateProfileAsync(
            actorUserId,
            candidateUserId,
            cancellationToken);

        return ToActionResult(response, response.Success, response.ErrorCode);
    }

    private ActionResult<EmployerCandidateProfileResponse> ToActionResult(
        EmployerCandidateProfileResponse response,
        bool success,
        string errorCode)
    {
        if (success)
            return Ok(response);

        return errorCode switch
        {
            EmployerCandidateErrorCodes.NotFound => NotFound(response),
            EmployerCandidateErrorCodes.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                response),
            EmployerCandidateErrorCodes.Conflict => Conflict(response),
            _ => BadRequest(response)
        };
    }
}
