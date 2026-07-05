using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Ai;
using GloryLikeBackend.Models.SkillAndJob;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Data;

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

    public DbSet<JobOffer> JobOffers { get; set; }

    public DbSet<SkillQuestionnaire> SkillQuestionnaires { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.UserName)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.Name)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.Surname)
                .HasMaxLength(80)
                .IsRequired();

            entity.Property(x => x.FatherName)
                .HasMaxLength(80);

            entity.Property(x => x.PhoneNumber)
                .HasMaxLength(30)
                .IsRequired();

            entity.Property(x => x.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.PasswordResetCodeHash)
                .HasMaxLength(500);

            entity.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("UX_Users_Email");

            entity.HasIndex(x => x.PhoneNumber)
                .IsUnique()
                .HasDatabaseName("UX_Users_PhoneNumber");

            entity.HasIndex(x => x.UserName)
                .IsUnique()
                .HasDatabaseName("UX_Users_UserName");
        });

        modelBuilder.Entity<SkillQuestionnaire>(entity =>
        {
            entity.ToTable("SkillQuestionnaires");

            entity.HasKey(x => x.Id);

            entity.Property(x => x.SkillId)
                .IsRequired(false);

            entity.Property(x => x.SkillName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Seniority)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.SkillComplexity)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.Language)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(x => x.QuestionCount)
                .IsRequired();

            entity.Property(x => x.StructureJson)
                .IsRequired();

            entity.Property(x => x.Version)
                .IsRequired();

            entity.Property(x => x.GeneratedByModel)
                .HasMaxLength(50);

            entity.Property(x => x.Status)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.SkillName,
                x.Seniority,
                x.SkillComplexity,
                x.Language,
                x.Version,
                x.Status
            }).HasDatabaseName("IX_SkillQuestionnaires_CacheLookup");
        });
    }
}
