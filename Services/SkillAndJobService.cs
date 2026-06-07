using GloryLikeBackend.Data;
using GloryLikeBackend.Models.SkillAndJob;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services
{
    public class SkillAndJobService : ISkillAndJobService
    {
        private readonly AppDbContext _context;

        public SkillAndJobService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<List<JobFamily>> GetAllJobFamiliesAsync()
        {
            return await _context.JobFamilies
        .Include(x => x.Seniorities)
        .ThenInclude(x => x.Positions)
        .ThenInclude(x => x.Skills)
        .ToListAsync();
        }


        public async Task AddJobFamiliesAsync(string jobName)
        {
            var jobFamily = new JobFamily
            {
                JobName = jobName
            };

            await _context.JobFamilies.AddAsync(jobFamily);
            await _context.SaveChangesAsync();
        }
    }
}
