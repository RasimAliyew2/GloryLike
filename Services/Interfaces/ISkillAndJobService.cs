using GloryLikeBackend.Models.SkillAndJob;

namespace GloryLikeBackend.Services.Interfaces;

public interface ISkillAndJobService
{
    Task<List<JobFamily>> GetAllJobFamiliesAsync();

    Task<List<SkillLookupDto>> GetAllSkillsAsync();

    Task AddJobFamiliesAsync(string jobName);
}
