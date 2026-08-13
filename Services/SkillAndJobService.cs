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

    public async Task<List<JobFamilyTaxonomyDto>>
        GetAllJobFamiliesAsync()
    {
        var jobFamilies = await _context.JobFamilies
            .AsNoTracking()
            .OrderBy(job => job.JobName)
            .ToListAsync();

        var positions = await _context.Positions
            .AsNoTracking()
            .AsSplitQuery()
            .Include(position => position.Skills)
            .Include(position => position.SeniorityLinks)
            .ThenInclude(link => link.Seniority)
            .OrderBy(position => position.Name)
            .ToListAsync();

        var positionsByJobFamily = positions
            .GroupBy(position => position.JobFamilyId)
            .ToDictionary(
                group => group.Key,
                group => group.ToList());

        return jobFamilies
            .Select(jobFamily => new JobFamilyTaxonomyDto
            {
                Id = jobFamily.Id,
                JobName = jobFamily.JobName,
                Positions = positionsByJobFamily.TryGetValue(
                    jobFamily.Id,
                    out var jobPositions)
                    ? jobPositions
                        .Select(BuildPositionDto)
                        .ToList()
                    : new List<PositionTaxonomyDto>()
            })
            .ToList();
    }

    public async Task<List<SkillLookupDto>> GetAllSkillsAsync()
    {
        var rows = await (
            from skill in _context.Skills.AsNoTracking()
            join position in _context.Positions.AsNoTracking()
                on skill.PositionId equals position.Id
            join jobFamily in _context.JobFamilies.AsNoTracking()
                on position.JobFamilyId equals jobFamily.Id
            where skill.Id > 0
                  && skill.SkillName != null
                  && skill.SkillName != string.Empty
            orderby skill.SkillName
            select new
            {
                Skill = skill,
                Position = position,
                JobFamily = jobFamily
            })
            .ToListAsync();

        var positionSeniorityLinks = await _context.PositionSeniorities
            .AsNoTracking()
            .Include(link => link.Seniority)
            .ToListAsync();

        var senioritiesByPosition = positionSeniorityLinks
            .GroupBy(link => link.PositionId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(link => link.Seniority.SortOrder)
                    .ThenBy(link => link.Seniority.Name)
                    .Select(link => new SeniorityOptionDto
                    {
                        Id = link.Seniority.Id,
                        Name = link.Seniority.Name,
                        SortOrder = link.Seniority.SortOrder
                    })
                    .ToList());

        return rows.Select(row => new SkillLookupDto
            {
                Id = row.Skill.Id,
                SkillName = row.Skill.SkillName,
                PositionId = row.Position.Id,
                PositionName = row.Position.Name,
                JobFamilyId = row.JobFamily.Id,
                JobFamilyName = row.JobFamily.JobName,
                Seniorities = senioritiesByPosition.TryGetValue(
                    row.Position.Id,
                    out var seniorities)
                    ? seniorities
                    : new List<SeniorityOptionDto>()
            })
            .ToList();
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

    private static PositionTaxonomyDto BuildPositionDto(
        Position position)
    {
        var skills = position.Skills
            .Where(skill =>
                skill.Id > 0
                && !string.IsNullOrWhiteSpace(skill.SkillName))
            .OrderBy(skill => skill.SkillName)
            .Select(skill => new SkillTaxonomyDto
            {
                Id = skill.Id,
                SkillName = skill.SkillName,
                PositionId = position.Id
            })
            .ToList();

        return new PositionTaxonomyDto
        {
            Id = position.Id,
            Name = position.Name,
            JobFamilyId = position.JobFamilyId,
            Seniorities = position.SeniorityLinks
                .OrderBy(link => link.Seniority.SortOrder)
                .ThenBy(link => link.Seniority.Name)
                .Select(link => new SeniorityTaxonomyDto
                {
                    Id = link.Seniority.Id,
                    Name = link.Seniority.Name,
                    SortOrder = link.Seniority.SortOrder,
                    // Skill entity-ləri Position-a bağlıdır. DTO-da eyni
                    // siyahı hər seniority altında göstərilir ki, JSON sırası
                    // JobFamily -> Position -> Seniority -> Skills olsun.
                    Skills = skills
                        .Select(skill => new SkillTaxonomyDto
                        {
                            Id = skill.Id,
                            SkillName = skill.SkillName,
                            PositionId = skill.PositionId
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
