using GloryLikeBackend.Models.SkillAndJob;

namespace GloryLikeBackend.Services.Interfaces;

public interface ISkillAndJobService
{
    Task<List<JobFamilyTaxonomyDto>> GetAllJobFamiliesAsync();

    Task<List<SkillLookupDto>> GetAllSkillsAsync();

    Task AddJobFamiliesAsync(string jobName);
}
