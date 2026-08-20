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
    public string? CompanyType { get; set; }

    [StringLength(120)]
    public string? ActivityScope { get; set; }

    [Range(1800, 2100)]
    public int? FoundationYear { get; set; }

    [StringLength(30)]
    public string? EmployeeCount { get; set; }

    [StringLength(240)]
    public string? Website { get; set; }

    [StringLength(40)]
    public string? PageLanguage { get; set; }

    [StringLength(240)]
    public string? CompanyVideo { get; set; }

    [StringLength(2500)]
    public string? CompanyDescription { get; set; }

    [StringLength(1600)]
    public string? CompanyCulture { get; set; }

    [StringLength(1600)]
    public string? WhyWorkWithUs { get; set; }

    public List<string>? Benefits { get; set; }

    [StringLength(500000)]
    public string? LogoDataUrl { get; set; }

    [StringLength(1100000)]
    public string? CoverImageDataUrl { get; set; }

    [StringLength(1000)]
    public string? AboutPageLayoutJson { get; set; }

    [StringLength(60000)]
    public string? AboutPageCustomHtml { get; set; }

    public bool UseCustomAboutPageHtml { get; set; }

    public List<CompanyLocationInput>? Locations { get; set; }

    [StringLength(240)]
    public string? CompanyAddress { get; set; }

    [StringLength(100)]
    public string? CompanyCountry { get; set; }

    [StringLength(100)]
    public string? CompanyCity { get; set; }

    [StringLength(240)]
    public string? LinkedInUrl { get; set; }

    [StringLength(240)]
    public string? InstagramUrl { get; set; }

    [StringLength(240)]
    public string? FacebookUrl { get; set; }

    [StringLength(240)]
    public string? YoutubeUrl { get; set; }

    [StringLength(240)]
    public string? TelegramUrl { get; set; }

    [StringLength(240)]
    public string? TiktokUrl { get; set; }
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
    public string LogoDataUrl { get; set; } = string.Empty;
    public string CoverImageDataUrl { get; set; } = string.Empty;
    public string AboutPageLayoutJson { get; set; } = "[]";
    public string AboutPageCustomHtml { get; set; } = string.Empty;
    public bool UseCustomAboutPageHtml { get; set; }
    public List<CompanyLocationDto> Locations { get; set; } = [];
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

public sealed class CompanyLocationInput
{
    public int? Id { get; set; }

    [StringLength(120)]
    public string? Name { get; set; }

    [StringLength(240)]
    public string? Address { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(100)]
    public string? City { get; set; }
}

public sealed class CompanyLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

public static class CompanyProfileErrorCodes
{
    public const string Validation = "validation";
    public const string Forbidden = "forbidden";
    public const string NotFound = "not_found";
    public const string Persistence = "persistence";
}

public sealed class PublicCompanyProfileResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CompanyOwnerUserId { get; set; }
    public CompanyProfileDto? Profile { get; set; }
    public List<PublicCompanyVacancyDto> Vacancies { get; set; } = [];
}

public sealed class PublicCompanyVacancyDto
{
    public int Id { get; set; }
    public string PlatformVacancyId { get; set; } = string.Empty;
    public string RoleTitle { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public string JobFamilyName { get; set; } = string.Empty;
    public string SeniorityName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public decimal? MinSalary { get; set; }
    public decimal? MaxSalary { get; set; }
    public string Currency { get; set; } = string.Empty;
    public bool HideSalary { get; set; }
    public DateTime? ApplicationDeadline { get; set; }
    public DateTime? PublishDate { get; set; }
}

public sealed class CustomizeCompanyAboutPageRequest
{
    [Range(1, int.MaxValue)]
    public int ActorUserId { get; set; }

    [Required]
    [StringLength(1500, MinimumLength = 3)]
    public string Prompt { get; set; } = string.Empty;

    [Required]
    [StringLength(60000)]
    public string CurrentHtml { get; set; } = string.Empty;
}

public sealed class CustomizeCompanyAboutPageResponse
{
    public bool Success { get; set; }
    public bool Allowed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
}
