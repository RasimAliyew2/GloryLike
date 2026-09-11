using GloryLikeBackend.Models;
using GloryLikeBackend.Models.Vacancies;
using Microsoft.EntityFrameworkCore;
namespace GloryLikeBackend.Data;
internal static class AutomationModelConfiguration
{
    internal static void ConfigureAutomations(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Vacancy>().Property(v => v.AutomationsJson).IsRequired().HasDefaultValue("[]");
        modelBuilder.Entity<Vacancy>().Property(v => v.AutomationEventVersion).HasDefaultValue(0).IsConcurrencyToken();
        modelBuilder.Entity<VacancyApplication>().Property(v => v.AutomationEventVersion).HasDefaultValue(0).IsConcurrencyToken();
        modelBuilder.Entity<CompanyAutomation>(e =>
        {
            e.ToTable("CompanyAutomations");e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(120);
            e.Property(x => x.RuleJson).IsRequired();
            e.HasIndex(x => x.CompanyOwnerUserId);
        });
        modelBuilder.Entity<AutomationEmailDelivery>(e =>
        {
            e.ToTable("AutomationEmailDeliveries");e.HasKey(x => x.Id);
            e.Property(x => x.EventKey).IsRequired().HasMaxLength(128);
            e.Property(x => x.EventType).IsRequired().HasMaxLength(32);
            e.Property(x => x.RuleName).IsRequired().HasMaxLength(120);
            e.Property(x => x.RecipientEmail).IsRequired().HasMaxLength(320);
            e.Property(x => x.Subject).IsRequired();
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.Status).IsRequired().HasMaxLength(16);
            e.Property(x => x.LastError).IsRequired().HasMaxLength(1000);
            e.HasIndex(x => x.CompanyOwnerUserId);
            e.HasIndex(x => new { x.EventKey,x.RuleId,x.ApplicationId }).IsUnique();
            e.HasIndex(x => new { x.Status,x.NextAttemptAtUtc });
        });
    }
}
