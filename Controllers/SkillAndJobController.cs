using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SkillAndJobController : ControllerBase
{
    private readonly ISkillAndJobService _skillAndJobService;

    public SkillAndJobController(
        ISkillAndJobService skillAndJobService)
    {
        _skillAndJobService = skillAndJobService;
    }

    [HttpGet("job-families")]
    public async Task<IActionResult> GetAllJobs()
    {
        var jobs =
            await _skillAndJobService.GetAllJobFamiliesAsync();

        return Ok(jobs);
    }

    [HttpGet("skills")]
    public async Task<IActionResult> GetAllSkills()
    {
        var skills =
            await _skillAndJobService.GetAllSkillsAsync();

        return Ok(skills);
    }
}
