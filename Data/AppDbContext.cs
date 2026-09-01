using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Ai;
using GloryLikeBackend.Models.Profile;
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

    public DbSet<PendingEmailRegistration> PendingEmailRegistrations
    {
        get;
        set;
    }

    public DbSet<CompanyTeamInvitation> CompanyTeamInvitations
    {
        get;
        set;
    }

    public DbSet<CompanyAccessRole> CompanyAccessRoles { get; set; }

    public DbSet<CompanyAccessRolePermission> CompanyAccessRolePermissions
    {
        get;
        set;
    }

    public DbSet<CompanyAccessAuditEvent> CompanyAccessAuditEvents
    {
        get;
        set;
    }

    public DbSet<CompanyCandidateMessage> CompanyCandidateMessages
    {
        get;
        set;
    }

    public DbSet<MicrosoftCalendarConnection> MicrosoftCalendarConnections
    {
        get;
        set;
    }

    public DbSet<InterviewMeeting> InterviewMeetings { get; set; }

    public DbSet<CompanyProfile> CompanyProfiles { get; set; }

    public DbSet<CompanyLocation> CompanyLocations { get; set; }

    public DbSet<CompanyStructureDepartment> CompanyStructureDepartments
    {
        get;
        set;
    }

    public DbSet<CompanyStructureDivision> CompanyStructureDivisions
    {
        get;
        set;
    }

    public DbSet<CompanyStructurePosition> CompanyStructurePositions
    {
        get;
        set;
    }

    public DbSet<CompanyHiringPlan> CompanyHiringPlans { get; set; }

    public DbSet<JobFamily> JobFamilies { get; set; }

    public DbSet<Seniority> Seniorities { get; set; }

    public DbSet<Position> Positions { get; set; }

    public DbSet<PositionSeniority> PositionSeniorities { get; set; }

    public DbSet<Skill> Skills { get; set; }

    public DbSet<JobOffer> JobOffers { get; set; }

    public DbSet<SkillQuestionnaire> SkillQuestionnaires { get; set; }

    public DbSet<UserSkill> UserSkills { get; set; }

    public DbSet<UserJob> UserJobs { get; set; }

    public DbSet<Vacancy> Vacancies { get; set; }

    public DbSet<VacancyApplication> VacancyApplications { get; set; }

    public DbSet<CandidateNotification> CandidateNotifications { get; set; }

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

    public DbSet<VacancyScreeningChoice> VacancyScreeningChoices { get; set; }

    public DbSet<VacancyScreeningAnswer> VacancyScreeningAnswers { get; set; }

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
                .HasMaxLength(30);

            entity.Property(x => x.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(x => x.AccountType)
                .HasMaxLength(20)
                .HasDefaultValue("candidate")
                .IsRequired();

            entity.Property(x => x.CompanyName)
                .HasMaxLength(150);

            entity.Property(x => x.CompanyType)
                .HasMaxLength(30);

            entity.Property(x => x.Industry)
                .HasMaxLength(120);

            entity.Property(x => x.BirthDate)
                .HasColumnType("date");

            entity.Property(x => x.About)
                .HasMaxLength(1000);

            entity.Property(x => x.ProfileImageDataUrl)
                .HasColumnType("nvarchar(max)");

            entity.Property(x => x.PasswordResetCodeHash)
                .HasMaxLength(500);

            entity.HasIndex(x => x.Email)
                .IsUnique()
                .HasDatabaseName("UX_Users_Email");

            entity.HasIndex(x => x.PhoneNumber)
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL")
                .HasDatabaseName("UX_Users_PhoneNumber");

            entity.HasIndex(x => x.UserName)
                .IsUnique()
                .HasDatabaseName("UX_Users_UserName");
        });

        modelBuilder.Entity<PendingEmailRegistration>(entity =>
        {
            entity.ToTable("PendingEmailRegistrations");

            entity.HasKey(item => item.Id);

            entity.Property(item => item.Email)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(item => item.PasswordHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(item => item.ProfileName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(item => item.AccountType)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(item => item.CompanyName)
                .HasMaxLength(150);

            entity.Property(item => item.CompanyType)
                .HasMaxLength(30);

            entity.Property(item => item.Industry)
                .HasMaxLength(120);

            entity.Property(item => item.VerificationCodeHash)
                .HasMaxLength(500)
                .IsRequired();

            entity.HasIndex(item => item.Email)
                .IsUnique()
                .HasDatabaseName(
                    "UX_PendingEmailRegistrations_Email");

            entity.HasIndex(item => item.VerificationCodeExpiresAtUtc)
                .HasDatabaseName(
                    "IX_PendingEmailRegistrations_ExpiresAtUtc");

            entity.HasIndex(item => item.TeamInvitationId)
                .HasDatabaseName(
                    "IX_PendingEmailRegistrations_TeamInvitationId");

            entity.HasOne<CompanyTeamInvitation>()
                .WithMany()
                .HasForeignKey(item => item.TeamInvitationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        ConfigureCompanyTeamInvitations(modelBuilder);
        ConfigureCompanyAccess(modelBuilder);
        ConfigureCompanyCandidateMessages(modelBuilder);
        ConfigureMicrosoftCalendar(modelBuilder);
        ConfigureCompanyProfiles(modelBuilder);
        ConfigureCompanyLocations(modelBuilder);
        ConfigureCompanyStructure(modelBuilder);
        ConfigureCompanyHiringPlans(modelBuilder);
        ConfigureJobTaxonomy(modelBuilder);

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

        ConfigureUserSkills(modelBuilder);
        ConfigureUserJobs(modelBuilder);
        ConfigureVacancies(modelBuilder);
    }

    private static void ConfigureCompanyCandidateMessages(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyCandidateMessage>(entity =>
        {
            entity.ToTable(
                "CompanyCandidateMessages",
                table => table.HasCheckConstraint(
                    "CK_CompanyCandidateMessages_DifferentUsers",
                    "[SenderUserId] <> [RecipientUserId]"));
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Body)
                .HasMaxLength(4000)
                .IsRequired();
            entity.Property(item => item.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(item => new
                {
                    item.CompanyOwnerUserId,
                    item.RecipientUserId,
                    item.ReadAtUtc
                })
                .HasDatabaseName(
                    "IX_CompanyCandidateMessages_RecipientUnread");

            entity.HasIndex(item => new
                {
                    item.CompanyOwnerUserId,
                    item.CandidateUserId,
                    item.CreatedAtUtc
                })
                .HasDatabaseName(
                    "IX_CompanyCandidateMessages_CandidateThread");

            entity.HasOne(item => item.CompanyOwner)
                .WithMany()
                .HasForeignKey(item => item.CompanyOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Sender)
                .WithMany()
                .HasForeignKey(item => item.SenderUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Recipient)
                .WithMany()
                .HasForeignKey(item => item.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Candidate)
                .WithMany()
                .HasForeignKey(item => item.CandidateUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureMicrosoftCalendar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MicrosoftCalendarConnection>(entity =>
        {
            entity.ToTable("MicrosoftCalendarConnections");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.MicrosoftUserId)
                .HasMaxLength(200)
                .IsRequired();
            entity.Property(item => item.TenantId)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.Email)
                .HasMaxLength(320)
                .IsRequired();
            entity.Property(item => item.DisplayName)
                .HasMaxLength(200);
            entity.Property(item => item.ProtectedAccessToken)
                .HasColumnType("nvarchar(max)")
                .IsRequired();
            entity.Property(item => item.ProtectedRefreshToken)
                .HasColumnType("nvarchar(max)")
                .IsRequired();
            entity.Property(item => item.GrantedScopes)
                .HasMaxLength(1000);
            entity.HasIndex(item => item.UserId)
                .IsUnique()
                .HasDatabaseName("UX_MicrosoftCalendarConnections_UserId");
            entity.HasOne(item => item.User)
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InterviewMeeting>(entity =>
        {
            entity.ToTable("InterviewMeetings");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Subject)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.CandidateEmail)
                .HasMaxLength(320)
                .IsRequired();
            entity.Property(item => item.GraphEventId)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(item => item.WebLink)
                .HasMaxLength(2000);
            entity.Property(item => item.JoinUrl)
                .HasMaxLength(2000);
            entity.Property(item => item.TransactionId)
                .HasMaxLength(64)
                .IsRequired();
            entity.HasIndex(item => item.TransactionId)
                .IsUnique()
                .HasDatabaseName("UX_InterviewMeetings_TransactionId");
            entity.HasIndex(item => new
                {
                    item.VacancyApplicationId,
                    item.StartAtUtc
                })
                .HasDatabaseName("IX_InterviewMeetings_Application_StartAtUtc");
            entity.HasOne(item => item.VacancyApplication)
                .WithMany()
                .HasForeignKey(item => item.VacancyApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(item => item.OrganizerUser)
                .WithMany()
                .HasForeignKey(item => item.OrganizerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureJobTaxonomy(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobFamily>(entity =>
        {
            entity.ToTable("JobFamilies");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.JobName)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(item => item.JobName)
                .IsUnique()
                .HasDatabaseName("UX_JobFamilies_JobName");
        });

        modelBuilder.Entity<Position>(entity =>
        {
            entity.ToTable("Positions");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(item => item.JobFamilyId)
                .HasDatabaseName("IX_Positions_JobFamilyId");

            entity.HasIndex(item => new
                {
                    item.JobFamilyId,
                    item.Name
                })
                .IsUnique()
                .HasDatabaseName("UX_Positions_JobFamilyId_Name");

            entity.HasOne(item => item.JobFamily)
                .WithMany(item => item.Positions)
                .HasForeignKey(item => item.JobFamilyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Seniority>(entity =>
        {
            entity.ToTable("Seniorities");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(item => item.SortOrder)
                .IsRequired();

            entity.HasIndex(item => item.Name)
                .IsUnique()
                .HasDatabaseName("UX_Seniorities_Name");

            entity.HasIndex(item => item.SortOrder)
                .IsUnique()
                .HasDatabaseName("UX_Seniorities_SortOrder");
        });

        modelBuilder.Entity<PositionSeniority>(entity =>
        {
            entity.ToTable("PositionSeniorities");
            entity.HasKey(item => new
                {
                    item.PositionId,
                    item.SeniorityId
                });

            entity.HasIndex(item => item.SeniorityId)
                .HasDatabaseName("IX_PositionSeniorities_SeniorityId");

            entity.HasOne(item => item.Position)
                .WithMany(item => item.SeniorityLinks)
                .HasForeignKey(item => item.PositionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(item => item.Seniority)
                .WithMany(item => item.PositionLinks)
                .HasForeignKey(item => item.SeniorityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.ToTable("Skills");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.SkillName)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(item => item.MinimumSenioritySortOrder)
                .HasDefaultValue(1)
                .IsRequired();

            entity.Property(item => item.IsCore)
                .HasDefaultValue(false)
                .IsRequired();

            entity.Property(item => item.IsActive)
                .HasDefaultValue(true)
                .IsRequired();

            entity.Property(item => item.AssessmentType)
                .HasMaxLength(10)
                .HasDefaultValue("TP")
                .IsRequired();

            entity.Property(item => item.VerificationMethod)
                .HasMaxLength(120)
                .HasDefaultValue(string.Empty)
                .IsRequired();

            entity.HasIndex(item => item.PositionId)
                .HasDatabaseName("IX_Skills_PositionId");

            entity.HasIndex(item => new
                {
                    item.PositionId,
                    item.SkillName
                })
                .IsUnique()
                .HasDatabaseName("UX_Skills_PositionId_SkillName");

            entity.HasOne(item => item.Position)
                .WithMany(item => item.Skills)
                .HasForeignKey(item => item.PositionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCompanyTeamInvitations(
        ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyTeamInvitation>(entity =>
        {
            entity.ToTable("CompanyTeamInvitations");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Email)
                .HasMaxLength(150)
                .IsRequired();
            entity.Property(item => item.Role)
                .HasMaxLength(80)
                .IsRequired();
            entity.Property(item => item.Status)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.HasIndex(item => item.AcceptedUserId)
                .HasDatabaseName(
                    "IX_CompanyTeamInvitations_AcceptedUserId");
            entity.HasIndex(item => new
                {
                    item.OwnerUserId,
                    item.Email
                })
                .IsUnique()
                .HasDatabaseName(
                    "UX_CompanyTeamInvitations_Owner_Email");
            entity.HasIndex(item => item.TokenHash)
                .IsUnique()
                .HasDatabaseName(
                    "UX_CompanyTeamInvitations_TokenHash");
            entity.HasIndex(item => item.AccessRoleId)
                .HasDatabaseName(
                    "IX_CompanyTeamInvitations_AccessRoleId");

            entity.HasOne(item => item.OwnerUser)
                .WithMany()
                .HasForeignKey(item => item.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.AcceptedUser)
                .WithMany()
                .HasForeignKey(item => item.AcceptedUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.AccessRole)
                .WithMany()
                .HasForeignKey(item => item.AccessRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCompanyAccess(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyAccessRole>(entity =>
        {
            entity.ToTable("CompanyAccessRoles");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name)
                .HasMaxLength(80)
                .IsRequired();
            entity.Property(item => item.Description)
                .HasMaxLength(500)
                .IsRequired();
            entity.Property(item => item.Scope)
                .HasMaxLength(30)
                .IsRequired();
            entity.HasIndex(item => new { item.OwnerUserId, item.Name })
                .IsUnique()
                .HasDatabaseName("UX_CompanyAccessRoles_Owner_Name");
            entity.HasOne(item => item.OwnerUser)
                .WithMany()
                .HasForeignKey(item => item.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompanyAccessRolePermission>(entity =>
        {
            entity.ToTable("CompanyAccessRolePermissions");
            entity.HasKey(item => new { item.RoleId, item.PermissionKey });
            entity.Property(item => item.PermissionKey)
                .HasMaxLength(100)
                .IsRequired();
            entity.HasOne(item => item.Role)
                .WithMany(item => item.Permissions)
                .HasForeignKey(item => item.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyAccessAuditEvent>(entity =>
        {
            entity.ToTable("CompanyAccessAuditEvents");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.EventType)
                .HasMaxLength(40)
                .IsRequired();
            entity.Property(item => item.Summary)
                .HasMaxLength(300)
                .IsRequired();
            entity.Property(item => item.Details)
                .HasMaxLength(1200)
                .IsRequired();
            entity.HasIndex(item => new { item.OwnerUserId, item.CreatedAtUtc })
                .HasDatabaseName("IX_CompanyAccessAuditEvents_Owner_CreatedAt");
            entity.HasIndex(item => item.ActorUserId)
                .HasDatabaseName("IX_CompanyAccessAuditEvents_ActorUserId");
            entity.HasIndex(item => item.TargetUserId)
                .HasDatabaseName("IX_CompanyAccessAuditEvents_TargetUserId");
        });
    }

    private static void ConfigureUserSkills(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserSkill>(entity =>
        {
            entity.ToTable("UserSkills");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.SkillName)
                .HasMaxLength(150)
                .IsRequired();
            entity.Property(item => item.PositionName)
                .HasMaxLength(150)
                .IsRequired();
            entity.Property(item => item.SeniorityName)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.JobFamilyName)
                .HasMaxLength(150)
                .IsRequired();
            entity.Property(item => item.SkillComplexity)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(item => item.Status)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(item => item.TaskComplexity)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.OwnershipLevel)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.DepthTier)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(item => item.UserId)
                .HasDatabaseName("IX_UserSkills_UserId");
            entity.HasIndex(item => item.JobFamilyId)
                .HasDatabaseName("IX_UserSkills_JobFamilyId");
            entity.HasIndex(item => new
                {
                    item.UserId,
                    item.SkillName
                })
                .IsUnique()
                .HasDatabaseName("UX_UserSkills_UserId_SkillName");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<JobFamily>()
                .WithMany()
                .HasForeignKey(item => item.JobFamilyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureUserJobs(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserJob>(entity =>
        {
            entity.ToTable("UserJobs");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.JobFamilyName)
                .HasMaxLength(150)
                .IsRequired();

            entity.HasIndex(item => item.UserId)
                .IsUnique()
                .HasDatabaseName("UX_UserJobs_UserId");
            entity.HasIndex(item => item.JobFamilyId)
                .HasDatabaseName("IX_UserJobs_JobFamilyId");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<JobFamily>()
                .WithMany()
                .HasForeignKey(item => item.JobFamilyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCompanyProfiles(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyProfile>(entity =>
        {
            entity.ToTable("CompanyProfiles");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.CompanyName)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(item => item.CompanyType)
                .HasMaxLength(40)
                .IsRequired();
            entity.Property(item => item.ActivityScope)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(item => item.EmployeeCount)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(item => item.Website)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.PageLanguage)
                .HasMaxLength(40)
                .IsRequired();
            entity.Property(item => item.CompanyVideo)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.CompanyDescription)
                .HasMaxLength(2500)
                .IsRequired();
            entity.Property(item => item.CompanyCulture)
                .HasMaxLength(1600)
                .IsRequired();
            entity.Property(item => item.WhyWorkWithUs)
                .HasMaxLength(1600)
                .IsRequired();
            entity.Property(item => item.BenefitsJson)
                .IsRequired();
            entity.Property(item => item.LogoDataUrl)
                .IsRequired();
            entity.Property(item => item.CoverImageDataUrl)
                .IsRequired();
            entity.Property(item => item.AboutPageLayoutJson)
                .HasMaxLength(1000)
                .IsRequired();
            entity.Property(item => item.AboutPageCustomHtml)
                .HasMaxLength(60000)
                .IsRequired();
            entity.Property(item => item.CompanyAddress)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.CompanyCountry)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.CompanyCity)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.LinkedInUrl)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.InstagramUrl)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.FacebookUrl)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.YoutubeUrl)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.TelegramUrl)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.TiktokUrl)
                .HasMaxLength(240)
                .IsRequired();

            entity.HasIndex(item => item.OwnerUserId)
                .IsUnique()
                .HasDatabaseName("UX_CompanyProfiles_OwnerUserId");

            entity.HasOne(item => item.OwnerUser)
                .WithMany()
                .HasForeignKey(item => item.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureCompanyLocations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyLocation>(entity =>
        {
            entity.ToTable("CompanyLocations");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Name)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(item => item.Address)
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(item => item.Country)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(item => item.CompanyProfileId)
                .HasDatabaseName("IX_CompanyLocations_CompanyProfileId");

            entity.HasOne(item => item.CompanyProfile)
                .WithMany(item => item.Locations)
                .HasForeignKey(item => item.CompanyProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCompanyStructure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyStructureDepartment>(entity =>
        {
            entity.ToTable("CompanyStructureDepartments");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name)
                .HasMaxLength(120)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.CompanyOwnerUserId,
                    item.Name
                })
                .IsUnique()
                .HasDatabaseName("UX_CompanyStructureDepartments_Owner_Name");
            entity.HasIndex(item => new
                {
                    item.CompanyOwnerUserId,
                    item.SortOrder
                })
                .HasDatabaseName("IX_CompanyStructureDepartments_Owner_SortOrder");
            entity.HasOne(item => item.CompanyOwnerUser)
                .WithMany()
                .HasForeignKey(item => item.CompanyOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompanyStructureDivision>(entity =>
        {
            entity.ToTable("CompanyStructureDivisions");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name)
                .HasMaxLength(120)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.DepartmentId,
                    item.Name
                })
                .IsUnique()
                .HasDatabaseName("UX_CompanyStructureDivisions_Department_Name");
            entity.HasIndex(item => new
                {
                    item.DepartmentId,
                    item.SortOrder
                })
                .HasDatabaseName("IX_CompanyStructureDivisions_Department_SortOrder");
            entity.HasOne(item => item.Department)
                .WithMany(item => item.Divisions)
                .HasForeignKey(item => item.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyStructurePosition>(entity =>
        {
            entity.ToTable(
                "CompanyStructurePositions",
                table => table.HasCheckConstraint(
                    "CK_CompanyStructurePositions_Headcount",
                    "[Headcount] >= 1 AND [Headcount] <= 10000"));
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(item => item.Seniority)
                .HasMaxLength(50)
                .HasDefaultValue("Not specified")
                .IsRequired();
            entity.Property(item => item.Headcount)
                .HasDefaultValue(1)
                .IsRequired();
            entity.Property(item => item.ReportsTo)
                .HasMaxLength(160)
                .HasDefaultValue(string.Empty)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.DivisionId,
                    item.Name
                })
                .IsUnique()
                .HasDatabaseName("UX_CompanyStructurePositions_Division_Name");
            entity.HasIndex(item => new
                {
                    item.DivisionId,
                    item.SortOrder
                })
                .HasDatabaseName("IX_CompanyStructurePositions_Division_SortOrder");
            entity.HasOne(item => item.Division)
                .WithMany(item => item.Positions)
                .HasForeignKey(item => item.DivisionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureCompanyHiringPlans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyHiringPlan>(entity =>
        {
            entity.ToTable("CompanyHiringPlans");
            entity.HasKey(item => item.Id);

            entity.Property(item => item.Priority)
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(item => item.DepartmentName)
                .HasMaxLength(120)
                .IsRequired();
            entity.Property(item => item.PositionName)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(item => item.EmploymentType)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(item => item.Notes)
                .HasMaxLength(1000)
                .IsRequired();

            entity.HasIndex(item => item.CompanyOwnerUserId)
                .HasDatabaseName("IX_CompanyHiringPlans_CompanyOwnerUserId");
            entity.HasIndex(item => item.JobFamilyId)
                .HasDatabaseName("IX_CompanyHiringPlans_JobFamilyId");
            entity.HasIndex(item => item.PositionId)
                .HasDatabaseName("IX_CompanyHiringPlans_PositionId");
            entity.HasIndex(item => item.SeniorityId)
                .HasDatabaseName("IX_CompanyHiringPlans_SeniorityId");

            entity.HasOne(item => item.CompanyOwnerUser)
                .WithMany()
                .HasForeignKey(item => item.CompanyOwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CreatedByUser)
                .WithMany()
                .HasForeignKey(item => item.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.JobFamily)
                .WithMany()
                .HasForeignKey(item => item.JobFamilyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Position)
                .WithMany()
                .HasForeignKey(item => item.PositionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Seniority)
                .WithMany()
                .HasForeignKey(item => item.SeniorityId)
                .OnDelete(DeleteBehavior.Restrict);
        });
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
            entity.Property(item => item.LocationName)
                .HasMaxLength(460)
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
            entity.HasIndex(item => item.CompanyOwnerUserId)
                .HasDatabaseName("IX_Vacancies_CompanyOwnerUserId");
            entity.HasIndex(item => item.PositionId)
                .HasDatabaseName("IX_Vacancies_PositionId");
            entity.HasIndex(item => item.HiringPlanId)
                .HasDatabaseName("IX_Vacancies_HiringPlanId");
            entity.HasIndex(item => item.CompanyLocationId)
                .HasDatabaseName("IX_Vacancies_CompanyLocationId");
            entity.HasIndex(item => item.CreatedAtUtc)
                .HasDatabaseName("IX_Vacancies_CreatedAtUtc");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.EmployerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.CompanyOwnerUserId)
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
            entity.HasOne(item => item.HiringPlan)
                .WithMany(item => item.Vacancies)
                .HasForeignKey(item => item.HiringPlanId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.CompanyLocation)
                .WithMany()
                .HasForeignKey(item => item.CompanyLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VacancyApplication>(entity =>
        {
            entity.ToTable("VacancyApplications");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Status)
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(item => item.FunnelStageName)
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(item => item.AppliedAtUtc)
                .IsRequired();
            entity.Property(item => item.UpdatedAtUtc)
                .IsRequired();

            entity.HasIndex(item => item.CandidateUserId)
                .HasDatabaseName("IX_VacancyApplications_CandidateUserId");
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.FunnelStageName
                })
                .HasDatabaseName(
                    "IX_VacancyApplications_VacancyId_FunnelStageName");
            entity.HasIndex(item => item.HiredAtUtc)
                .HasDatabaseName("IX_VacancyApplications_HiredAtUtc");
            entity.HasIndex(item => new
                {
                    item.VacancyId,
                    item.CandidateUserId
                })
                .IsUnique()
                .HasDatabaseName(
                    "UX_VacancyApplications_VacancyId_CandidateUserId");

            entity.HasOne(item => item.Vacancy)
                .WithMany(item => item.Applications)
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.CandidateUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CandidateNotification>(entity =>
        {
            entity.ToTable("CandidateNotifications");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Type)
                .HasMaxLength(50)
                .IsRequired();
            entity.Property(item => item.Title)
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(item => item.Message)
                .HasMaxLength(600)
                .IsRequired();
            entity.Property(item => item.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(item => new
                {
                    item.CandidateUserId,
                    item.IsRead,
                    item.CreatedAtUtc
                })
                .HasDatabaseName(
                    "IX_CandidateNotifications_Candidate_Read_CreatedAt");
            entity.HasIndex(item => item.VacancyApplicationId)
                .HasDatabaseName(
                    "IX_CandidateNotifications_VacancyApplicationId");

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(item => item.CandidateUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Vacancy>()
                .WithMany()
                .HasForeignKey(item => item.VacancyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<VacancyApplication>()
                .WithMany()
                .HasForeignKey(item => item.VacancyApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
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

        modelBuilder.Entity<VacancyScreeningChoice>(entity =>
        {
            entity.ToTable("VacancyScreeningChoices");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ChoiceText)
                .HasMaxLength(300)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.ScreeningQuestionId,
                    item.SortOrder
                })
                .HasDatabaseName("IX_VacancyScreeningChoices_QuestionId");
            entity.HasOne(item => item.ScreeningQuestion)
                .WithMany(item => item.Choices)
                .HasForeignKey(item => item.ScreeningQuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VacancyScreeningAnswer>(entity =>
        {
            entity.ToTable("VacancyScreeningAnswers");
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
            entity.Property(item => item.AnswerValueJson)
                .HasMaxLength(4000)
                .IsRequired();
            entity.Property(item => item.AnswerDisplayText)
                .HasMaxLength(4000)
                .IsRequired();
            entity.HasIndex(item => new
                {
                    item.VacancyApplicationId,
                    item.ScreeningQuestionId
                })
                .IsUnique()
                .HasDatabaseName("UX_VacancyScreeningAnswers_ApplicationId_QuestionId");
            entity.HasOne(item => item.VacancyApplication)
                .WithMany(item => item.ScreeningAnswers)
                .HasForeignKey(item => item.VacancyApplicationId)
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
