using GloryLikeBackend.Models.SkillAndJob;

namespace GloryLikeBackend.Services.Interfaces
{
    public interface ISkillAndJobService
    {
        public Task<List<JobFamily>> GetAllJobFamiliesAsync();
        public Task AddJobFamiliesAsync(string JobName);
       
    }
}
