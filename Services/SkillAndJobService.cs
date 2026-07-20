using GloryLikeBackend.Data;
using GloryLikeBackend.Models.SkillAndJob;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class SkillAndJobService : ISkillAndJobService
{
    private readonly AppDbContext _context;

    public SkillAndJobService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<JobFamily>> GetAllJobFamiliesAsync()
    {
        return await _context.JobFamilies
            .AsNoTracking()
            .Include(job => job.Seniorities)
            .ThenInclude(seniority => seniority.Positions)
            .ThenInclude(position => position.Skills)
            .OrderBy(job => job.JobName)
            .ToListAsync();
    }

    public async Task<List<SkillLookupDto>> GetAllSkillsAsync()
    {
        // Skill search nested JobFamily JSON-dan asılı deyil.
        // SQL Skills cədvəli birbaşa Position, Seniority və JobFamily
        // cədvəlləri ilə join edilir.
        return await (
            from skill in _context.Skills.AsNoTracking()
            join position in _context.Positions.AsNoTracking()
                on skill.PositionId equals position.Id
            join seniority in _context.Seniorities.AsNoTracking()
                on position.SeniorityId equals seniority.Id
            join jobFamily in _context.JobFamilies.AsNoTracking()
                on seniority.JobFamilyId equals jobFamily.Id
            where skill.Id > 0
                  && skill.SkillName != null
                  && skill.SkillName != string.Empty
            orderby skill.SkillName
            select new SkillLookupDto
            {
                Id = skill.Id,
                SkillName = skill.SkillName,
                PositionId = position.Id,
                PositionName = position.Name,
                SeniorityId = seniority.Id,
                SeniorityName = seniority.Name,
                JobFamilyId = jobFamily.Id,
                JobFamilyName = jobFamily.JobName
            })
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
