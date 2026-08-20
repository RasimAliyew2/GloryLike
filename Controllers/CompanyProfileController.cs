using GloryLikeBackend.Dtos.CompanyProfile;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/company/profile")]
public sealed class CompanyProfileController : ControllerBase
{
    private readonly ICompanyProfileService _companyProfileService;
    private readonly IOpenAiCompanyAboutPageDesigner _aboutPageDesigner;

    public CompanyProfileController(
        ICompanyProfileService companyProfileService,
        IOpenAiCompanyAboutPageDesigner aboutPageDesigner)
    {
        _companyProfileService = companyProfileService;
        _aboutPageDesigner = aboutPageDesigner;
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

    [HttpGet("public/{companyOwnerUserId:int}")]
    public async Task<ActionResult<PublicCompanyProfileResponse>> GetPublic(
        int companyOwnerUserId,
        CancellationToken cancellationToken)
    {
        var response = await _companyProfileService.GetPublicAsync(
            companyOwnerUserId,
            cancellationToken);

        return response.Success ? Ok(response) : NotFound(response);
    }

    [HttpPost("about-html/ai")]
    public async Task<ActionResult<CustomizeCompanyAboutPageResponse>>
        CustomizeWithAi(
            [FromBody] CustomizeCompanyAboutPageRequest request,
            CancellationToken cancellationToken)
    {
        var response = await _aboutPageDesigner.CustomizeAsync(
            request,
            cancellationToken);

        return response.Success ? Ok(response) : BadRequest(response);
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
