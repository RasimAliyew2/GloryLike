using GloryLikeBackend.Dtos.EmployerCandidates;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/messages")]
public sealed class CompanyMessagesController : ControllerBase
{
    private readonly IEmployerCandidateMessagingService _service;

    public CompanyMessagesController(
        IEmployerCandidateMessagingService service)
    {
        _service = service;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<CompanyMessagingOverviewResponse>> GetOverview(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetOverviewAsync(actorUserId, cancellationToken);
        return Map(response, response.Success, response.ErrorCode);
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<CompanyUnreadCountResponse>> GetUnreadCount(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetUnreadCountAsync(actorUserId, cancellationToken);
        return Map(response, response.Success, response.ErrorCode);
    }

    [HttpGet("thread")]
    public async Task<ActionResult<CompanyMessageThreadResponse>> GetThread(
        [FromQuery] int actorUserId,
        [FromQuery] int otherUserId,
        [FromQuery] int candidateUserId,
        CancellationToken cancellationToken)
    {
        var response = await _service.GetThreadAsync(
            actorUserId,
            otherUserId,
            candidateUserId,
            cancellationToken);
        return Map(response, response.Success, response.ErrorCode);
    }

    [HttpPost]
    public async Task<ActionResult<CompanyMessageActionResponse>> Send(
        [FromBody] SendCompanyCandidateMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var response = await _service.SendAsync(request, cancellationToken);
        return Map(response, response.Success, response.ErrorCode);
    }

    [HttpPost("read")]
    public async Task<ActionResult<CompanyMessageActionResponse>> MarkRead(
        [FromBody] MarkCompanyMessageThreadReadRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var response = await _service.MarkThreadReadAsync(request, cancellationToken);
        return Map(response, response.Success, response.ErrorCode);
    }

    private ActionResult<T> Map<T>(T response, bool success, string errorCode)
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
