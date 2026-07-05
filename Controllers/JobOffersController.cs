using GloryLikeBackend.Dtos.JobOffers;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobOffersController : ControllerBase
{
    private readonly IJobOfferService _jobOfferService;

    public JobOffersController(IJobOfferService jobOfferService)
    {
        _jobOfferService = jobOfferService;
    }

    [HttpGet]
    public async Task<ActionResult<List<JobOfferDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _jobOfferService.GetAllAsync(cancellationToken);
        return Ok(result);
    }
}
