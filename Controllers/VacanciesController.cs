using GloryLikeBackend.Dtos.Vacancies;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class VacanciesController : ControllerBase
{
    private readonly IVacancyService _vacancyService;

    public VacanciesController(IVacancyService vacancyService)
    {
        _vacancyService = vacancyService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(CreateVacancyResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(CreateVacancyResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(CreateVacancyResponse),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreateVacancyResponse>> Create(
        [FromBody] CreateVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vacancyService.CreateAsync(
            request,
            cancellationToken);

        var response = new CreateVacancyResponse
        {
            Success = result.Success,
            Message = result.Message,
            VacancyId = result.VacancyId,
            PlatformVacancyId = result.PlatformVacancyId,
            CreatedAtUtc = result.CreatedAtUtc
        };

        if (result.Success)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                response);
        }

        return result.FailureKind
            == CreateVacancyFailureKind.Conflict
                ? Conflict(response)
                : BadRequest(response);
    }
}
