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

    [HttpGet("employer/{employerUserId:int}")]
    [ProducesResponseType(
        typeof(EmployerVacancyListResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(EmployerVacancyListResponse),
        StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EmployerVacancyListResponse>>
        GetEmployerVacancies(
            int employerUserId,
            CancellationToken cancellationToken)
    {
        if (employerUserId <= 0)
        {
            return BadRequest(new EmployerVacancyListResponse
            {
                Success = false,
                Message = "Employer user ID düzgün deyil.",
                EmployerUserId = employerUserId
            });
        }

        var vacancies = await _vacancyService.GetEmployerVacanciesAsync(
            employerUserId,
            cancellationToken);

        return Ok(new EmployerVacancyListResponse
        {
            Success = true,
            Message = vacancies.Count == 0
                ? "Employer üçün vacancy tapılmadı."
                : $"{vacancies.Count} vacancy tapıldı.",
            EmployerUserId = employerUserId,
            Vacancies = vacancies
        });
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
