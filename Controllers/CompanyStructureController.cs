using GloryLikeBackend.Dtos.CompanyStructure;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/structure")]
public sealed class CompanyStructureController : ControllerBase
{
    private const long MaxExcelFileBytes = 5 * 1024 * 1024;
    private readonly ICompanyStructureService _service;

    public CompanyStructureController(ICompanyStructureService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyStructureResponse>> Get(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.GetAsync(
            actorUserId,
            cancellationToken));
    }

    [HttpPut]
    public async Task<ActionResult<CompanyStructureResponse>> Save(
        [FromBody] SaveCompanyStructureRequest request,
        CancellationToken cancellationToken)
    {
        return ToActionResult(await _service.SaveAsync(
            request,
            cancellationToken));
    }

    [HttpPost("import")]
    [RequestSizeLimit(MaxExcelFileBytes)]
    public async Task<ActionResult<CompanyStructureResponse>> Import(
        [FromQuery] int actorUserId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new CompanyStructureResponse
            {
                Success = false,
                Message = "Select a non-empty .xlsx file.",
                ErrorCode = CompanyStructureErrorCodes.Import
            });
        }

        if (file.Length > MaxExcelFileBytes
            || !string.Equals(
                Path.GetExtension(file.FileName),
                ".xlsx",
                StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new CompanyStructureResponse
            {
                Success = false,
                Message = "Only .xlsx files up to 5 MB are supported.",
                ErrorCode = CompanyStructureErrorCodes.Import
            });
        }

        await using var stream = file.OpenReadStream();
        return ToActionResult(await _service.ImportAsync(
            actorUserId,
            stream,
            cancellationToken));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await _service.ExportAsync(actorUserId, cancellationToken);
        if (!result.Success)
        {
            var response = new CompanyStructureResponse
            {
                Success = false,
                Message = result.Message,
                ErrorCode = result.ErrorCode
            };
            return result.ErrorCode == CompanyStructureErrorCodes.Forbidden
                ? StatusCode(StatusCodes.Status403Forbidden, response)
                : BadRequest(response);
        }

        return File(
            result.Content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            result.FileName);
    }

    private ActionResult<CompanyStructureResponse> ToActionResult(
        CompanyStructureResponse response)
    {
        if (response.Success)
            return Ok(response);

        return response.ErrorCode switch
        {
            CompanyStructureErrorCodes.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                response),
            _ => BadRequest(response)
        };
    }
}
