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

    [HttpGet("candidate/{candidateUserId:int}")]
    [ProducesResponseType(
        typeof(CandidateVacancyListResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(CandidateVacancyListResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(CandidateVacancyListResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CandidateVacancyListResponse>>
        GetCandidateVacancies(
            int candidateUserId,
            CancellationToken cancellationToken)
    {
        if (candidateUserId <= 0)
        {
            return BadRequest(new CandidateVacancyListResponse
            {
                Success = false,
                Message = "Candidate user ID düzgün deyil.",
                CandidateUserId = candidateUserId
            });
        }

        var response = await _vacancyService.GetCandidateVacanciesAsync(
            candidateUserId,
            cancellationToken);

        if (response is null)
        {
            return NotFound(new CandidateVacancyListResponse
            {
                Success = false,
                Message = "Candidate user SQL-də tapılmadı.",
                CandidateUserId = candidateUserId
            });
        }

        return Ok(response);
    }

    [HttpPost("{vacancyId:int}/applications")]
    [ProducesResponseType(
        typeof(ApplyToVacancyResponse),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        typeof(ApplyToVacancyResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ApplyToVacancyResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ApplyToVacancyResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplyToVacancyResponse>> Apply(
        int vacancyId,
        [FromBody] ApplyToVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vacancyService.ApplyToVacancyAsync(
            vacancyId,
            request.CandidateUserId,
            cancellationToken);

        var response = new ApplyToVacancyResponse
        {
            Success = result.Success,
            Message = result.Message,
            VacancyId = result.VacancyId,
            CandidateUserId = result.CandidateUserId,
            ApplicationId = result.ApplicationId,
            Status = result.Status,
            AppliedAtUtc = result.AppliedAtUtc,
            AlreadyApplied = result.AlreadyApplied
        };

        if (result.Success)
        {
            return result.AlreadyApplied
                ? Ok(response)
                : StatusCode(StatusCodes.Status201Created, response);
        }

        return result.FailureKind == ApplyToVacancyFailureKind.NotFound
            ? NotFound(response)
            : BadRequest(response);
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

    [HttpGet("employer/{employerUserId:int}/{vacancyId:int}")]
    [ProducesResponseType(
        typeof(EmployerVacancyDetailResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(EmployerVacancyDetailResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(EmployerVacancyDetailResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployerVacancyDetailResponse>>
        GetEmployerVacancyDetail(
            int employerUserId,
            int vacancyId,
            CancellationToken cancellationToken)
    {
        if (employerUserId <= 0 || vacancyId <= 0)
        {
            return BadRequest(new EmployerVacancyDetailResponse
            {
                Success = false,
                Message = "Employer və vacancy ID düzgün deyil.",
                EmployerUserId = employerUserId
            });
        }

        var response = await _vacancyService.GetEmployerVacancyDetailAsync(
            employerUserId,
            vacancyId,
            cancellationToken);

        if (response is null)
        {
            return NotFound(new EmployerVacancyDetailResponse
            {
                Success = false,
                Message = "Vacancy tapılmadı və ya bu employer-ə aid deyil.",
                EmployerUserId = employerUserId
            });
        }

        return Ok(response);
    }

    [HttpGet("employer/{employerUserId:int}/{vacancyId:int}/edit")]
    [ProducesResponseType(
        typeof(EmployerVacancyEditResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(EmployerVacancyEditResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(EmployerVacancyEditResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EmployerVacancyEditResponse>>
        GetEmployerVacancyForEdit(
            int employerUserId,
            int vacancyId,
            CancellationToken cancellationToken)
    {
        if (employerUserId <= 0 || vacancyId <= 0)
        {
            return BadRequest(new EmployerVacancyEditResponse
            {
                Success = false,
                Message = "Employer və vacancy ID düzgün deyil.",
                EmployerUserId = employerUserId,
                VacancyId = vacancyId
            });
        }

        var response = await _vacancyService.GetEmployerVacancyForEditAsync(
            employerUserId,
            vacancyId,
            cancellationToken);

        if (response is null)
        {
            return NotFound(new EmployerVacancyEditResponse
            {
                Success = false,
                Message = "Vacancy tapılmadı və ya bu employer-ə aid deyil.",
                EmployerUserId = employerUserId,
                VacancyId = vacancyId
            });
        }

        return Ok(response);
    }

    [HttpPost("{vacancyId:int}/employer-status/toggle")]
    [ProducesResponseType(
        typeof(ToggleEmployerVacancyStatusResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(ToggleEmployerVacancyStatusResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(ToggleEmployerVacancyStatusResponse),
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ToggleEmployerVacancyStatusResponse>>
        ToggleEmployerStatus(
            int vacancyId,
            [FromBody] ToggleEmployerVacancyStatusRequest request,
            CancellationToken cancellationToken)
    {
        var result = await _vacancyService.ToggleEmployerStatusAsync(
            request.EmployerUserId,
            vacancyId,
            cancellationToken);

        var response = new ToggleEmployerVacancyStatusResponse
        {
            Success = result.Success,
            Message = result.Message,
            VacancyId = result.VacancyId,
            EmployerUserId = result.EmployerUserId,
            Status = result.Status,
            IsSuspended = result.IsSuspended,
            UpdatedAtUtc = result.UpdatedAtUtc
        };

        if (result.Success)
            return Ok(response);

        return result.FailureKind
            == ToggleEmployerVacancyStatusFailureKind.NotFound
                ? NotFound(response)
                : BadRequest(response);
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

    [HttpPut("{vacancyId:int}")]
    [ProducesResponseType(
        typeof(UpdateVacancyResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        typeof(UpdateVacancyResponse),
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        typeof(UpdateVacancyResponse),
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        typeof(UpdateVacancyResponse),
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UpdateVacancyResponse>> Update(
        int vacancyId,
        [FromBody] CreateVacancyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _vacancyService.UpdateAsync(
            vacancyId,
            request,
            cancellationToken);

        var response = new UpdateVacancyResponse
        {
            Success = result.Success,
            Message = result.Message,
            VacancyId = result.VacancyId,
            EmployerUserId = result.EmployerUserId,
            PlatformVacancyId = result.PlatformVacancyId,
            Status = result.Status,
            UpdatedAtUtc = result.UpdatedAtUtc
        };

        if (result.Success)
            return Ok(response);

        return result.FailureKind switch
        {
            UpdateVacancyFailureKind.NotFound => NotFound(response),
            UpdateVacancyFailureKind.Conflict => Conflict(response),
            _ => BadRequest(response)
        };
    }
}
