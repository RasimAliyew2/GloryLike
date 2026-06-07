using GloryLikeBackend.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using GloryLikeBackend.Models.SkillAndJob;
namespace GloryLikeBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<JobFamily> JobFamilies { get; set; }
        public DbSet<Seniority> Seniorities { get; set; }
        public DbSet<Position> Positions { get; set; }

        public DbSet<Skill> Skills { get; set; }
    }
}
