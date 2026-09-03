using GloryLikeBackend.Dtos.CompanyTemplates;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/templates")]
public sealed class CompanyTemplatesController : ControllerBase
{
    private readonly ICompanyTemplateService _service;

    public CompanyTemplatesController(ICompanyTemplateService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyTemplateResponse>> Get(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.GetAsync(
            actorUserId,
            cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyTemplateResponse>> Create(
        [FromBody] SaveCompanyTemplateRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(request, cancellationToken);
        return response.Success
            ? StatusCode(StatusCodes.Status201Created, response)
            : ToActionResult(response);
    }

    [HttpPut("{templateId:guid}")]
    public async Task<ActionResult<CompanyTemplateResponse>> Update(
        Guid templateId,
        [FromBody] SaveCompanyTemplateRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.UpdateAsync(
            templateId,
            request,
            cancellationToken));
    }

    [HttpDelete("{templateId:guid}")]
    public async Task<ActionResult<CompanyTemplateResponse>> Delete(
        Guid templateId,
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.DeleteAsync(
            actorUserId,
            templateId,
            cancellationToken));
    }

    private ActionResult<CompanyTemplateResponse> ToActionResult(
        CompanyTemplateResponse response)
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
