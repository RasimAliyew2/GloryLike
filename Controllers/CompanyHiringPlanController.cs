using GloryLikeBackend.Dtos.CompanyHiringPlan;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/hiring-plan")]
public sealed class CompanyHiringPlanController : ControllerBase
{
    private const long MaxExcelFileBytes = 5 * 1024 * 1024;
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

    [HttpPost("import")]
    [RequestSizeLimit(MaxExcelFileBytes)]
    public async Task<ActionResult<CompanyHiringPlanResponse>> Import(
        [FromQuery] int actorUserId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new CompanyHiringPlanResponse
            {
                Success = false,
                Message = "Select a non-empty .xlsx file.",
                ErrorCode = CompanyHiringPlanErrorCodes.Validation
            });
        }

        if (file.Length > MaxExcelFileBytes
            || !string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new CompanyHiringPlanResponse
            {
                Success = false,
                Message = "Only .xlsx files up to 5 MB are supported.",
                ErrorCode = CompanyHiringPlanErrorCodes.Validation
            });
        }

        await using var stream = file.OpenReadStream();
        return ToActionResult(await _service.ImportAsync(
            actorUserId,
            stream,
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
