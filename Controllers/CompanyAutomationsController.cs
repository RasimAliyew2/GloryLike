using GloryLikeBackend.Services;
using System.Security.Cryptography;
using System.Text;
using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/automation-templates")]
public sealed class CompanyAutomationsController : ControllerBase
{
    private readonly CompanyAutomationService _service;
    private readonly string _secret;

    public CompanyAutomationsController(CompanyAutomationService service, IConfiguration configuration)
    {
        _service = service;
        _secret = configuration["SocialAuth:BackendSharedSecret"] ?? string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyAutomationResponse>> Get(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        return ToActionResult(await _service.GetAsync(
            actorUserId,
            cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyAutomationResponse>> Create(
        [FromBody] SaveCompanyAutomationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        var response = await _service.SaveAsync(null, request, cancellationToken);
        return response.Success
            ? StatusCode(StatusCodes.Status201Created, response)
            : ToActionResult(response);
    }

    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult<CompanyAutomationResponse>> Update(
        Guid templateId,
        [FromBody] SaveCompanyAutomationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        return ToActionResult(await _service.SaveAsync(
            templateId,
            request,
            cancellationToken));
    }

    [HttpDelete("{templateId:guid}")]
    public async Task<ActionResult<CompanyAutomationResponse>> Delete(
        Guid templateId,
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        return ToActionResult(await _service.DeleteAsync(
            actorUserId,
            templateId,
            cancellationToken));
    }

    [HttpPost("deliveries/{deliveryId:guid}/retry")]
    public async Task<ActionResult<CompanyAutomationResponse>> Retry(Guid deliveryId, [FromQuery] int actorUserId, CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        return ToActionResult(await _service.RetryAsync(actorUserId, deliveryId, cancellationToken));
    }

    // Actor IDs are accepted only from the authenticated WebApp server.
    private bool IsTrusted()
    {
        var supplied = Request.Headers["X-BothFind-Backend-Secret"].ToString();
        return !string.IsNullOrWhiteSpace(_secret) && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(_secret), Encoding.UTF8.GetBytes(supplied));
    }

    private ActionResult<CompanyAutomationResponse> ToActionResult(
        CompanyAutomationResponse response)
    {
        if (response.Success)
            return Ok(response);

        return response.ErrorCode switch
        {
            CompanyTemplateErrorCodes.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                response),
            CompanyTemplateErrorCodes.NotFound => NotFound(response),
            CompanyTemplateErrorCodes.Conflict => Conflict(response),
            _ => BadRequest(response)
        };
    }
}
