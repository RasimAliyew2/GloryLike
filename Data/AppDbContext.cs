using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Ai;
using GloryLikeBackend.Models.SkillAndJob;
using GloryLikeBackend.Models.Vacancies;
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

    public DbSet<Vacancy> Vacancies { get; set; }

    public DbSet<VacancySkillRequirement> VacancySkillRequirements { get; set; }

    public DbSet<VacancyBenefit> VacancyBenefits { get; set; }

    public DbSet<VacancyApplicationRequirement> VacancyApplicationRequirements
    {
        get;
        set;
    }

    public DbSet<VacancyScreeningQuestion> VacancyScreeningQuestions
    {
        get;
        set;
    }

    public DbSet<VacancyFunnelStage> VacancyFunnelStages { get; set; }

    public DbSet<VacancyPublicationChannel> VacancyPublicationChannels
    {
        get;
        set;
    }

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

        ConfigureVacancies(modelBuilder);
    }

    private static void ConfigureVacancies(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vacancy>(entity =>
        {
            entity.ToTable("Vacancies");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.PlatformVacancyId)
                .HasMaxLength(40)
                .IsRequired();
            entity.Property(item => item.JobFamilyName)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.SeniorityName)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.PositionName)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.RoleTitle)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.ClientRequisitionCode)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.EmploymentType)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.ExperienceRequired)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.EducationRequirement)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.EducationLevel)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.MinSalary)
                .HasPrecision(18, 2);
            entity.Property(item => item.MaxSalary)
                .HasPrecision(18, 2);
            entity.Property(item => item.PaymentTerms)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.Currency)
                .HasMaxLength(10)
                .IsRequired();
            entity.Property(item => item.JobDescription)
                .HasMaxLength(5000)
                .IsRequired();
            entity.Property(item => item.ScreeningNotes)
                .HasMaxLength(5000)
                .IsRequired();
            entity.Property(item => item.Visibility)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.ContactEmail)
                .HasMaxLength(150)
                .IsRequired();
            entity.Property(item => item.Status)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.SourcePayloadJson)
                .IsRequired();

            entity.HasIndex(item => item.PlatformVacancyId)
                .IsUnique()
                .HasDatabaseName("UX_Vacancies_PlatformVacancyId");
            entity.HasIndex(item => item.EmployerUserId)
                .HasDatabaseName("IX_Vacancies_EmployerUserId");
            entity.HasIndex(item => item.PositionId)
                .HasDatabaseName("IX_Vacancies_PositionId");
            entity.HasIndex(item => item.CreatedAtUtc)
                .HasDatabaseName("IX_Vacancies_CreatedAtUtc");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.EmployerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JobFamily>()
                .WithMany()
                .HasForeignKey(item => item.JobFamilyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Seniority>()
                .WithMany()
                .HasForeignKey(item => item.SeniorityId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Position>()
                .WithMany()
                .HasForeignKey(item => item.PositionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VacancySkillRequirement>(entity =>
        {
            entity.ToTable("VacancySkillRequirements");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.SkillName)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.RequirementType)
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.SkillId
                })
                .IsUnique()
                .HasDatabaseName("UX_VacancySkillRequirements_VacancyId_SkillId");
            entity.HasOne(item => item.Vacancy)
                .WithMany(item => item.SkillRequirements)
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Skill>()
                .WithMany()
                .HasForeignKey(item => item.SkillId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<VacancyBenefit>(entity =>
        {
            entity.ToTable("VacancyBenefits");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name)
                .HasMaxLength(100)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.Name
                })
                .IsUnique()
                .HasDatabaseName("UX_VacancyBenefits_VacancyId_Name");
            entity.HasOne(item => item.Vacancy)
                .WithMany(item => item.Benefits)
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VacancyApplicationRequirement>(entity =>
        {
            entity.ToTable("VacancyApplicationRequirements");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.FieldKey)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.Label)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.RequirementMode)
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.FieldKey
                })
                .IsUnique()
                .HasDatabaseName("UX_VacancyApplicationRequirements_VacancyId_FieldKey");
            entity.HasOne(item => item.Vacancy)
                .WithMany(item => item.ApplicationRequirements)
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VacancyScreeningQuestion>(entity =>
        {
            entity.ToTable("VacancyScreeningQuestions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.QuestionText)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(item => item.AnswerType)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.RequirementType)
                .HasMaxLength(20)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.SortOrder
                })
                .HasDatabaseName("IX_VacancyScreeningQuestions_VacancyId");
            entity.HasOne(item => item.Vacancy)
                .WithMany(item => item.ScreeningQuestions)
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VacancyFunnelStage>(entity =>
        {
            entity.ToTable("VacancyFunnelStages");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.StageName)
                .HasMaxLength(100)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.SortOrder
                })
                .HasDatabaseName("IX_VacancyFunnelStages_VacancyId");
            entity.HasOne(item => item.Vacancy)
                .WithMany(item => item.FunnelStages)
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VacancyPublicationChannel>(entity =>
        {
            entity.ToTable("VacancyPublicationChannels");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ChannelType)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.ChannelName)
                .HasMaxLength(50)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.ChannelName
                })
                .IsUnique()
                .HasDatabaseName("UX_VacancyPublicationChannels_VacancyId_ChannelName");
            entity.HasOne(item => item.Vacancy)
                .WithMany(item => item.PublicationChannels)
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
