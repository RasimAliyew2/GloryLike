namespace GloryLikeBackend.Models;

public sealed record CompanyLetterTemplateDefinition(
    Guid Id,
    string Key,
    string Name,
    string Audience,
    string Category,
    string Subject,
    string Body,
    int SortOrder);

public static class CompanyLetterTemplateCatalog
{
    public const string Candidate = "Candidate";
    public const string HiringManager = "Hiring Manager";
    public const string Recruiter = "Recruiter";

    public static readonly IReadOnlyList<string> Audiences =
    [
        Candidate,
        HiringManager,
        Recruiter
    ];

    public static readonly IReadOnlyList<string> Variables =
    [
        "{candidate_name}",
        "{vacancy_title}",
        "{company_name}",
        "{recruiter_name}",
        "{hiring_manager_name}",
        "{shortlist_link}",
        "{scorecard_link}"
    ];

    public static readonly IReadOnlyList<CompanyLetterTemplateDefinition> All =
    [
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10001"),
            "response-received",
            "Response received",
            Candidate,
            "Confirmation",
            "We received your application for {vacancy_title}",
            "Hello, {candidate_name}!\n\n"
            + "Thank you for applying for the {vacancy_title} position at {company_name}. "
            + "We will review your application and contact you soon.\n\n"
            + "Best regards,\nThe {company_name} team",
            10),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10002"),
            "interview-invitation",
            "Invitation to an interview",
            Candidate,
            "Interview",
            "Interview invitation — {vacancy_title}",
            "Hello, {candidate_name}!\n\n"
            + "We are pleased to invite you to an interview for the {vacancy_title} position. "
            + "Please choose a convenient time.\n\n"
            + "Best regards,\n{recruiter_name}",
            20),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10003"),
            "polite-rejection",
            "Polite rejection",
            Candidate,
            "Rejection",
            "Update on your application — {vacancy_title}",
            "Hello, {candidate_name}!\n\n"
            + "Thank you for your interest in the {vacancy_title} position. "
            + "At this stage, we have decided to continue with other candidates. "
            + "We wish you every success in your job search.\n\n"
            + "Best regards,\nThe {company_name} team",
            30),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10004"),
            "job-offer",
            "Offer",
            Candidate,
            "Offer",
            "Job offer — {vacancy_title}",
            "Hello, {candidate_name}!\n\n"
            + "We are delighted to offer you the {vacancy_title} position at {company_name}. "
            + "The full offer details are included in the attachment.\n\n"
            + "Best regards,\n{recruiter_name}",
            40),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10005"),
            "document-request",
            "Request documents",
            Candidate,
            "Documents",
            "Required documents — {vacancy_title}",
            "Hello, {candidate_name}!\n\n"
            + "To continue the recruitment process, please provide the requested documents.\n\n"
            + "Thank you!",
            50),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10006"),
            "shortlist-ready",
            "The shortlist is ready",
            HiringManager,
            "Review",
            "Candidate shortlist — {vacancy_title}",
            "Hello, {hiring_manager_name}!\n\n"
            + "The candidate shortlist for the {vacancy_title} position is ready. "
            + "Please review it and share your feedback.\n\n"
            + "Candidate link: {shortlist_link}",
            60),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10007"),
            "decision-needed",
            "Decision needed",
            HiringManager,
            "Decision",
            "Your decision is needed — {candidate_name}",
            "Hello, {hiring_manager_name}!\n\n"
            + "Candidate {candidate_name} is waiting for your decision regarding the "
            + "{vacancy_title} position.",
            70),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10008"),
            "hiring-manager-evaluation",
            "Evaluate the candidate after the interview",
            HiringManager,
            "Evaluation",
            "Evaluate candidate {candidate_name}",
            "Hello, {hiring_manager_name}!\n\n"
            + "The interview with {candidate_name} for the {vacancy_title} position has been completed. "
            + "Please submit your evaluation in the system.\n\n"
            + "Evaluate: {scorecard_link}",
            80),
        new(
            Guid.Parse("7a43b57b-1a18-4f50-a901-2cc6a9a10009"),
            "recruiter-evaluation",
            "Evaluate the candidate after the interview (recruiter)",
            Recruiter,
            "Evaluation",
            "Candidate evaluation — {candidate_name}",
            "Hello, {recruiter_name}!\n\n"
            + "The interview with {candidate_name} has been completed. "
            + "Please submit your evaluation.\n\n"
            + "Evaluate: {scorecard_link}",
            90)
    ];

    public static readonly IReadOnlyDictionary<Guid, CompanyLetterTemplateDefinition> ById =
        All.ToDictionary(item => item.Id);
}
