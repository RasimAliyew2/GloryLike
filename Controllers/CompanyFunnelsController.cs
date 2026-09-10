using System.Security.Cryptography;
using System.Text;
using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/funnel-templates")]
public sealed class CompanyFunnelsController : ControllerBase
{
    private readonly ICompanyFunnelService _service;
    private readonly string _secret;

    public CompanyFunnelsController(ICompanyFunnelService service, IConfiguration configuration)
    {
        _service = service;
        _secret = configuration["SocialAuth:BackendSharedSecret"] ?? string.Empty;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyFunnelResponse>> Get(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        return ToActionResult(await _service.GetAsync(
            actorUserId,
            cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyFunnelResponse>> Create(
        [FromBody] SaveCompanyFunnelRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        var response = await _service.CreateAsync(request, cancellationToken);
        return response.Success
            ? StatusCode(StatusCodes.Status201Created, response)
            : ToActionResult(response);
    }

    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult<CompanyFunnelResponse>> Update(
        Guid templateId,
        [FromBody] SaveCompanyFunnelRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsTrusted()) return Unauthorized();
        return ToActionResult(await _service.UpdateAsync(
            templateId,
            request,
            cancellationToken));
    }

    [HttpDelete("{templateId:guid}")]
    public async Task<ActionResult<CompanyFunnelResponse>> Delete(
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

    // Actor IDs are accepted only from the authenticated WebApp server.
    private bool IsTrusted()
    {
        var supplied = Request.Headers["X-BothFind-Backend-Secret"].ToString();
        return !string.IsNullOrWhiteSpace(_secret) && CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(_secret), Encoding.UTF8.GetBytes(supplied));
    }

    private ActionResult<CompanyFunnelResponse> ToActionResult(
        CompanyFunnelResponse response)
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
