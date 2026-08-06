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

        if (result.Success)
            return Ok(result);

        return result.ErrorCode switch
        {
            CompanyTeamErrorCodes.NotFound =>
                NotFound(result),
            CompanyTeamErrorCodes.Forbidden =>
                StatusCode(
                    StatusCodes.Status403Forbidden,
                    result),
            _ => BadRequest(result)
        };
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
            CompanyTeamErrorCodes.Forbidden =>
                StatusCode(
                    StatusCodes.Status403Forbidden,
                    result),
            _ => BadRequest(result)
        };
    }

    [HttpDelete("invitations/{invitationId:guid}")]
    public async Task<ActionResult<CompanyTeamResponse>>
        RemoveMember(
            Guid invitationId,
            [FromQuery] int actorUserId,
            CancellationToken cancellationToken)
    {
        var result =
            await _companyTeamService.RemoveMemberAsync(
                invitationId,
                actorUserId,
                cancellationToken);

        if (result.Success)
            return Ok(result);

        return result.ErrorCode switch
        {
            CompanyTeamErrorCodes.NotFound =>
                NotFound(result),
            CompanyTeamErrorCodes.Forbidden =>
                StatusCode(
                    StatusCodes.Status403Forbidden,
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
