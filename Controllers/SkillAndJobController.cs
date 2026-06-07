using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GloryLikeBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SkillAndJobController : Controller
    {
        private readonly ISkillAndJobService _skillAndJobService;

        public SkillAndJobController(ISkillAndJobService skillAndJobService)
        {
            _skillAndJobService = skillAndJobService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("job-families")]
        public async Task<IActionResult> GetAllJobs()
        {
            var Jobs = await _skillAndJobService.GetAllJobFamiliesAsync();
            return Ok(Jobs);
        }
    }
}
