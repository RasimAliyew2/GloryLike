using GloryLikeBackend.Dtos.CompanyTemplates;
namespace GloryLikeBackend.Models;

public sealed record CompanyFunnelTemplateDefinition(Guid Id, string Key, int SortOrder,
    string Name, string Description, List<FunnelTemplateStage> Stages);

public static class CompanyFunnelTemplateCatalog
{
    private static FunnelTemplateStage Stage(string name, int hours, string role) =>
        new() { StageName = name, Hours = hours, ResponsibleRole = role };
    public static readonly IReadOnlyList<CompanyFunnelTemplateDefinition> All =
    [
        new(new Guid("f0900000-0000-4000-8000-000000000001"), "standard", 0,
            "Standard (5 stages)", "Universal funnel for most positions.",
            [Stage("Applied",48,"Recruiter"), Stage("Screening",72,"Recruiter"),
             Stage("Interview",120,"Hiring Manager"), Stage("Offer",48,"Hiring Manager"), Stage("Hired",0,"HR")]),
        new(new Guid("f0900000-0000-4000-8000-000000000002"), "quick", 1,
            "Quick (3 stages)", "For mass or urgent hiring.",
            [Stage("Applied",24,"Recruiter"), Stage("Interview",72,"Hiring Manager"), Stage("Hired",0,"HR")]),
        new(new Guid("f0900000-0000-4000-8000-000000000003"), "technical", 2,
            "Technical (6 stages)", "For engineering and technical roles.",
            [Stage("Applied",48,"Recruiter"), Stage("Screening",72,"Recruiter"),
             Stage("Tech Interview",120,"Hiring Manager"), Stage("Final Interview",96,"Hiring Manager"),
             Stage("Offer",48,"HR"), Stage("Hired",0,"HR")]),
        new(new Guid("f0900000-0000-4000-8000-000000000004"), "hr-interview", 3,
            "With HR interview (6 stages)", "A separate HR stage before the final interview.",
            [Stage("Applied",48,"Recruiter"), Stage("Screening",72,"Recruiter"),
             Stage("HR Interview",72,"Recruiter"), Stage("Interview",120,"Hiring Manager"),
             Stage("Offer",48,"Hiring Manager"), Stage("Hired",0,"HR")])
    ];
    public static readonly IReadOnlyDictionary<Guid, CompanyFunnelTemplateDefinition> ById =
        All.ToDictionary(item => item.Id);
}
