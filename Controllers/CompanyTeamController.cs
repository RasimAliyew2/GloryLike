using GloryLikeBackend.Dtos.CompanyTeam;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/team")]
public sealed class CompanyTeamController : ControllerBase
{
    private readonly ICompanyTeamService _companyTeamService;

    public CompanyTeamController(
        ICompanyTeamService companyTeamService)
    {
        _companyTeamService = companyTeamService;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyTeamResponse>> GetTeam(
        [FromQuery] int ownerUserId,
        CancellationToken cancellationToken)
    {
        var result =
            await _companyTeamService.GetTeamAsync(
                ownerUserId,
                cancellationToken);

        return result.Success
            ? Ok(result)
            : NotFound(result);
    }

    [HttpPost("invitations")]
    public async Task<ActionResult<CompanyTeamResponse>> Invite(
        [FromBody] InviteCompanyTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result =
            await _companyTeamService.InviteAsync(
                request,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.ErrorCode switch
        {
            CompanyTeamErrorCodes.NotFound =>
                NotFound(result),
            CompanyTeamErrorCodes.DuplicateEmail
                or CompanyTeamErrorCodes.AlreadyAccepted
                or CompanyTeamErrorCodes.Conflict =>
                    Conflict(result),
            CompanyTeamErrorCodes.EmailDeliveryFailed =>
                StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    result),
            _ => BadRequest(result)
        };
    }

    [HttpGet("invitations/resolve")]
    public async Task<ActionResult<
        ResolveCompanyTeamInvitationResponse>>
        ResolveInvitation(
            [FromQuery] string token,
            CancellationToken cancellationToken)
    {
        var result =
            await _companyTeamService.ResolveInvitationAsync(
                token,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.ErrorCode switch
        {
            CompanyTeamErrorCodes.NotFound =>
                NotFound(result),
            CompanyTeamErrorCodes.Expired
                or CompanyTeamErrorCodes.AlreadyAccepted =>
                    StatusCode(
                        StatusCodes.Status410Gone,
                        result),
            _ => BadRequest(result)
        };
    }
}
