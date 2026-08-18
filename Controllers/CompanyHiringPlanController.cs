using GloryLikeBackend.Dtos.CompanyHiringPlan;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/hiring-plan")]
public sealed class CompanyHiringPlanController : ControllerBase
{
    private readonly ICompanyHiringPlanService _service;

    public CompanyHiringPlanController(ICompanyHiringPlanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyHiringPlanResponse>> Get(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.GetAsync(
            actorUserId,
            cancellationToken));
    }

    [HttpGet("{planId:int}")]
    public async Task<ActionResult<CompanyHiringPlanResponse>> GetById(
        int planId,
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.GetByIdAsync(
            actorUserId,
            planId,
            cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyHiringPlanResponse>> Create(
        [FromBody] SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(request, cancellationToken);
        return response.Success
            ? StatusCode(StatusCodes.Status201Created, response)
            : ToActionResult(response);
    }

    [HttpPut("{planId:int}")]
    public async Task<ActionResult<CompanyHiringPlanResponse>> Update(
        int planId,
        [FromBody] SaveCompanyHiringPlanRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.UpdateAsync(
            planId,
            request,
            cancellationToken));
    }

    [HttpDelete("{planId:int}")]
    public async Task<ActionResult<CompanyHiringPlanResponse>> Delete(
        int planId,
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.DeleteAsync(
            actorUserId,
            planId,
            cancellationToken));
    }

    private ActionResult<CompanyHiringPlanResponse> ToActionResult(
        CompanyHiringPlanResponse response)
    {
        if (response.Success)
            return Ok(response);

        return response.ErrorCode switch
        {
            CompanyHiringPlanErrorCodes.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                response),
            CompanyHiringPlanErrorCodes.NotFound => NotFound(response),
            CompanyHiringPlanErrorCodes.Conflict => Conflict(response),
            _ => BadRequest(response)
        };
    }
}
