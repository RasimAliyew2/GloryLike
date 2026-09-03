namespace GloryLikeBackend.Models;

public static class CompanyAccessPermissionCatalog
{
    public sealed record Permission(string Key, string Label, bool Sensitive = false);

    public sealed record Group(string Key, string Label, IReadOnlyList<Permission> Permissions);

    public static readonly IReadOnlyList<Group> Groups =
    [
        new("vacancies", "Vacancies",
        [
            new("vacancies.view", "View vacancies"),
            new("vacancies.create", "Creating vacancies"),
            new("vacancies.edit", "Editing vacancies"),
            new("vacancies.publish", "Publication of vacancies"),
            new("vacancies.fill", "Filling vacancies"),
            new("vacancies.delete", "Deleting vacancies", true),
            new("vacancies.assign_team", "Assigning a team to a vacancy"),
            new("vacancies.salary", "Salary range", true)
        ]),
        new("candidates", "Candidates",
        [
            new("candidates.view", "View candidates"),
            new("candidates.contacts", "Candidate contacts (PII)", true),
            new("candidates.unlock", "Unlocking the profile", true),
            new("candidates.edit", "Editing candidate data"),
            new("candidates.notes", "Candidate notes"),
            new("candidates.export", "Export candidate database", true)
        ]),
        new("funnel", "Funnel",
        [
            new("funnel.view", "Funnel view"),
            new("funnel.move", "Moving through the stages"),
            new("funnel.reject", "Rejecting a candidate"),
            new("funnel.mass_actions", "Mass actions"),
            new("funnel.configure", "Setting up funnel stages", true)
        ]),
        new("interview", "Interview",
        [
            new("interview.view", "View interviews"),
            new("interview.schedule", "Appointment of an interview"),
            new("interview.feedback", "Giving feedback"),
            new("interview.feedback_all", "Viewing someone else's feedback", true),
            new("interview.tasks_view", "View tasks"),
            new("interview.tasks_assign", "Assign tasks")
        ]),
        new("ai", "AI",
        [
            new("ai.shortlist", "AI Shortlist launch", true),
            new("ai.video_screening", "Starting a video screening", true),
            new("ai.skill_request", "Request a new skill")
        ]),
        new("analytics", "Analytics",
        [
            new("analytics.personal", "Personal analytics"),
            new("analytics.team", "Team analytics"),
            new("analytics.company", "Company analytics"),
            new("analytics.recruiter_kpi", "Recruiter profile (KPI)", true),
            new("analytics.talent_market", "Talent Market Panel"),
            new("analytics.export", "Export reports")
        ]),
        new("company", "Company",
        [
            new("company.profile_edit", "Edit your Business Profile"),
            new("company.structure_view", "View the Organizational Chart"),
            new("company.structure_edit", "Editing the Org Chart"),
            new("company.hiring_plan_view", "View a hiring plan"),
            new("company.hiring_plan_edit", "Editing a Hiring Plan", true),
            new("company.career_page_edit", "Editing a career page"),
            new("company.templates_manage", "Manage company templates")
        ]),
        new("team", "Team",
        [
            new("team.participants.view", "View participants"),
            new("team.participants.invite", "Invite participants"),
            new("team.participants.deactivate", "Deactivating a member", true),
            new("team.roles.assign", "Assign a role to a member", true),
            new("team.access.delegate", "Temporary delegation of access", true),
            new("team.roles.manage", "Manage roles", true)
        ]),
        new("billing", "Billing",
        [
            new("billing.view", "View billing"),
            new("billing.manage", "Subscription management", true)
        ])
    ];

    public static readonly IReadOnlyDictionary<string, Permission> ByKey =
        Groups
            .SelectMany(group => group.Permissions)
            .ToDictionary(
                permission => permission.Key,
                permission => permission,
                StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> AllKeys =
        new HashSet<string>(ByKey.Keys, StringComparer.OrdinalIgnoreCase);
}
