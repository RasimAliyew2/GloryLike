namespace GloryLikeBackend.Models;

public sealed class CompanyProfile
{
    public int Id { get; set; }

    public int OwnerUserId { get; set; }

    public User OwnerUser { get; set; } = null!;

    public string CompanyName { get; set; } = string.Empty;

    public string CompanyType { get; set; } = string.Empty;

    public string ActivityScope { get; set; } = string.Empty;

    public int? FoundationYear { get; set; }

    public string EmployeeCount { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string PageLanguage { get; set; } = string.Empty;

    public string CompanyVideo { get; set; } = string.Empty;

    public string CompanyDescription { get; set; } = string.Empty;

    public string CompanyCulture { get; set; } = string.Empty;

    public string WhyWorkWithUs { get; set; } = string.Empty;

    public string BenefitsJson { get; set; } = "[]";

    public string LogoDataUrl { get; set; } = string.Empty;

    public string CoverImageDataUrl { get; set; } = string.Empty;

    public string AboutPageLayoutJson { get; set; } = "[]";

    public string AboutPageCustomHtml { get; set; } = string.Empty;

    public bool UseCustomAboutPageHtml { get; set; }

    public string CompanyAddress { get; set; } = string.Empty;

    public string CompanyCountry { get; set; } = string.Empty;

    public string CompanyCity { get; set; } = string.Empty;

    public string LinkedInUrl { get; set; } = string.Empty;

    public string InstagramUrl { get; set; } = string.Empty;

    public string FacebookUrl { get; set; } = string.Empty;

    public string YoutubeUrl { get; set; } = string.Empty;

    public string TelegramUrl { get; set; } = string.Empty;

    public string TiktokUrl { get; set; } = string.Empty;

    public int UpdatedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<CompanyLocation> Locations { get; set; } = new();
}
