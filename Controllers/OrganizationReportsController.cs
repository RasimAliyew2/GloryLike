using GloryLikeBackend.Dtos.Reports;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/reports")]
public sealed class OrganizationReportsController : ControllerBase
{
    private readonly IOrganizationReportsService _reportsService;

    public OrganizationReportsController(
        IOrganizationReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [HttpGet]
    public async Task<ActionResult<OrganizationReportsResponse>> Get(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        var response = await _reportsService.GetAsync(
            actorUserId,
            cancellationToken);

        if (response.Success)
            return Ok(response);

        return response.ErrorCode == "forbidden"
            ? StatusCode(StatusCodes.Status403Forbidden, response)
            : NotFound(response);
    }
}
