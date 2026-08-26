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
    public async Task<ActionResult<OrganizationReportCatalogResponse>> GetCatalog(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        var response = await _reportsService.GetCatalogAsync(
            actorUserId,
            cancellationToken);

        return ToActionResult(response.Success, response.ErrorCode, response);
    }

    [HttpGet("vacancy-creation")]
    public async Task<ActionResult<VacancyCreationReportResponse>>
        ExecuteVacancyCreationReport(
            [FromQuery] int actorUserId,
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            CancellationToken cancellationToken)
    {
        var response = await _reportsService.ExecuteVacancyCreationReportAsync(
            actorUserId,
            dateFrom,
            dateTo,
            cancellationToken);

        return ToActionResult(response.Success, response.ErrorCode, response);
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<OrganizationAnalyticsDashboardResponse>>
        GetDashboard(
            [FromQuery] int actorUserId,
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTo,
            CancellationToken cancellationToken)
    {
        var response = await _reportsService.GetDashboardAsync(
            actorUserId,
            dateFrom,
            dateTo,
            cancellationToken);

        return ToActionResult(response.Success, response.ErrorCode, response);
    }

    [HttpGet("employees/{employeeUserId:int}")]
    public async Task<ActionResult<ReportEmployeeProfileResponse>>
        GetEmployeeProfile(
            int employeeUserId,
            [FromQuery] int actorUserId,
            CancellationToken cancellationToken)
    {
        var response = await _reportsService.GetEmployeeProfileAsync(
            actorUserId,
            employeeUserId,
            cancellationToken);

        return ToActionResult(response.Success, response.ErrorCode, response);
    }

    private ActionResult<TResponse> ToActionResult<TResponse>(
        bool success,
        string errorCode,
        TResponse response)
    {
        if (success)
            return Ok(response);

        return errorCode switch
        {
            ReportErrorCodes.Validation => BadRequest(response),
            ReportErrorCodes.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                response),
            _ => NotFound(response)
        };
    }
}

internal static class ReportErrorCodes
{
    public const string Validation = "validation";
    public const string Forbidden = "forbidden";
}
