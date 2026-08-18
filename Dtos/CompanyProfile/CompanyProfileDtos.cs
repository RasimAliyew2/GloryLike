using System.ComponentModel.DataAnnotations;

namespace GloryLikeBackend.Dtos.CompanyProfile;

public sealed class SaveCompanyProfileRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    [Required]
    [StringLength(150)]
    public string CompanyName { get; set; } = string.Empty;

    [StringLength(30)]
    public string CompanyType { get; set; } = string.Empty;

    [StringLength(120)]
    public string ActivityScope { get; set; } = string.Empty;

    [Range(1800, 2100)]
    public int? FoundationYear { get; set; }

    [StringLength(30)]
    public string EmployeeCount { get; set; } = string.Empty;

    [StringLength(240)]
    public string Website { get; set; } = string.Empty;

    [StringLength(40)]
    public string PageLanguage { get; set; } = string.Empty;

    [StringLength(240)]
    public string CompanyVideo { get; set; } = string.Empty;

    [StringLength(2500)]
    public string CompanyDescription { get; set; } = string.Empty;

    [StringLength(1600)]
    public string CompanyCulture { get; set; } = string.Empty;

    [StringLength(1600)]
    public string WhyWorkWithUs { get; set; } = string.Empty;

    public List<string> Benefits { get; set; } = [];

    [StringLength(240)]
    public string CompanyAddress { get; set; } = string.Empty;

    [StringLength(100)]
    public string CompanyCountry { get; set; } = string.Empty;

    [StringLength(100)]
    public string CompanyCity { get; set; } = string.Empty;

    [StringLength(240)]
    public string LinkedInUrl { get; set; } = string.Empty;

    [StringLength(240)]
    public string InstagramUrl { get; set; } = string.Empty;

    [StringLength(240)]
    public string FacebookUrl { get; set; } = string.Empty;

    [StringLength(240)]
    public string YoutubeUrl { get; set; } = string.Empty;

    [StringLength(240)]
    public string TelegramUrl { get; set; } = string.Empty;

    [StringLength(240)]
    public string TiktokUrl { get; set; } = string.Empty;
}

public sealed class CompanyProfileResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string ErrorCode { get; set; } = string.Empty;

    public int CompanyOwnerUserId { get; set; }

    public CompanyProfileDto? Profile { get; set; }
}

public sealed class CompanyProfileDto
{
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
    public List<string> Benefits { get; set; } = [];
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyCountry { get; set; } = string.Empty;
    public string CompanyCity { get; set; } = string.Empty;
    public string LinkedInUrl { get; set; } = string.Empty;
    public string InstagramUrl { get; set; } = string.Empty;
    public string FacebookUrl { get; set; } = string.Empty;
    public string YoutubeUrl { get; set; } = string.Empty;
    public string TelegramUrl { get; set; } = string.Empty;
    public string TiktokUrl { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
}

public static class CompanyProfileErrorCodes
{
    public const string Validation = "validation";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Persistence = "persistence";
}
