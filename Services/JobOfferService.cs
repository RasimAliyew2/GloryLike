using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.JobOffers;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public class JobOfferService : IJobOfferService
{
    private readonly AppDbContext _dbContext;

    public JobOfferService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<JobOfferDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.JobOffers
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new JobOfferDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                RequiredJob = x.RequiredJob,
                Seniority = x.Seniority,
                Skills = x.Skills,
                SkillsWeight = x.SkillsWeight
            })
            .ToListAsync(cancellationToken);
    }
}
