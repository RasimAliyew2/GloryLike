using GloryLikeBackend.Dtos.CompanyProfile;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/profile")]
public sealed class CompanyProfileController : ControllerBase
{
    private readonly ICompanyProfileService _companyProfileService;

    public CompanyProfileController(
        ICompanyProfileService companyProfileService)
    {
        _companyProfileService = companyProfileService;
    }

    [HttpGet]
    public async Task<ActionResult<CompanyProfileResponse>> Get(
        [FromQuery] int actorUserId,
        CancellationToken cancellationToken)
    {
        var response = await _companyProfileService.GetAsync(
            actorUserId,
            cancellationToken);

        return ToActionResult(response);
    }

    [HttpPut]
    public async Task<ActionResult<CompanyProfileResponse>> Save(
        [FromBody] SaveCompanyProfileRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _companyProfileService.SaveAsync(
            request,
            cancellationToken);

        return ToActionResult(response);
    }

    private ActionResult<CompanyProfileResponse> ToActionResult(
        CompanyProfileResponse response)
    {
        if (response.Success)
            return Ok(response);

        return response.ErrorCode switch
        {
            CompanyProfileErrorCodes.Forbidden => StatusCode(
                StatusCodes.Status403Forbidden,
                response),
            CompanyProfileErrorCodes.NotFound => NotFound(response),
            _ => BadRequest(response)
        };
    }
}
