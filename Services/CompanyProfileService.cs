using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyProfile;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyProfileService : ICompanyProfileService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly ILogger<CompanyProfileService> _logger;

    public CompanyProfileService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService,
        ILogger<CompanyProfileService> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _logger = logger;
    }

    public async Task<CompanyProfileResponse> GetAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);

        if (access is null)
            return Forbidden();

        var profile = await _dbContext.CompanyProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.OwnerUserId == access.CompanyOwnerUserId,
                cancellationToken);

        if (profile is not null)
        {
            return Successful(
                access.CompanyOwnerUserId,
                ToDto(profile),
                "Company profile SQL-dən yükləndi.");
        }

        var owner = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == access.CompanyOwnerUserId,
                cancellationToken);

        if (owner is null)
        {
            return Failed(
                "Company owner tapılmadı.",
                CompanyProfileErrorCodes.NotFound);
        }

        return Successful(
            access.CompanyOwnerUserId,
            new CompanyProfileDto
            {
                CompanyName = owner.CompanyName?.Trim()
                    ?? BuildDisplayName(owner),
                CompanyType = owner.CompanyType?.Trim()
                    ?? string.Empty,
                ActivityScope = owner.Industry?.Trim()
                    ?? string.Empty
            },
            "Company profile üçün ilkin məlumatlar yükləndi.");
    }

    public async Task<CompanyProfileResponse> SaveAsync(
        SaveCompanyProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(
            request.ActorUserId,
            cancellationToken);

        if (access is null)
            return Forbidden();

        Normalize(request);
        var validationMessage = Validate(request);

        if (!string.IsNullOrWhiteSpace(validationMessage))
        {
            return Failed(
                validationMessage,
                CompanyProfileErrorCodes.Validation);
        }

        var profile = await _dbContext.CompanyProfiles
            .FirstOrDefaultAsync(
                item => item.OwnerUserId == access.CompanyOwnerUserId,
                cancellationToken);

        var now = DateTime.UtcNow;

        if (profile is null)
        {
            profile = new CompanyProfile
            {
                OwnerUserId = access.CompanyOwnerUserId,
                CreatedAtUtc = now
            };

            _dbContext.CompanyProfiles.Add(profile);
        }

        Apply(request, profile);
        profile.UpdatedByUserId = request.ActorUserId;
        profile.UpdatedAtUtc = now;

        var companyUserIds = await _companyAccessService
            .GetActiveUserIdsAsync(
                access.CompanyOwnerUserId,
                cancellationToken);

        var companyUsers = await _dbContext.Users
            .Where(item => companyUserIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        foreach (var user in companyUsers)
        {
            user.CompanyName = request.CompanyName;
            user.CompanyType = EmptyToNull(request.CompanyType);
            user.Industry = EmptyToNull(request.ActivityScope);
            user.UpdatedAt = now;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Company profile could not be saved for owner {CompanyOwnerUserId}.",
                access.CompanyOwnerUserId);

            var schemaMissing = exception.InnerException?.Message.Contains(
                "Invalid object name",
                StringComparison.OrdinalIgnoreCase) == true;

            return Failed(
                schemaMissing
                    ? "Database schema is not updated. Run the latest Backend migrations."
                    : "Company profile could not be saved in SQL. Check Backend logs for the database error.",
                CompanyProfileErrorCodes.Persistence);
        }

        return Successful(
            access.CompanyOwnerUserId,
            ToDto(profile),
            "Company profile bütün team üçün yeniləndi.");
    }

    private static void Normalize(SaveCompanyProfileRequest request)
    {
        request.CompanyName = Clean(request.CompanyName);
        request.CompanyType = Clean(request.CompanyType);
        request.ActivityScope = Clean(request.ActivityScope);
        request.EmployeeCount = Clean(request.EmployeeCount);
        request.Website = Clean(request.Website);
        request.PageLanguage = Clean(request.PageLanguage);
        request.CompanyVideo = Clean(request.CompanyVideo);
        request.CompanyDescription = Clean(request.CompanyDescription);
        request.CompanyCulture = Clean(request.CompanyCulture);
        request.WhyWorkWithUs = Clean(request.WhyWorkWithUs);
        request.CompanyAddress = Clean(request.CompanyAddress);
        request.CompanyCountry = Clean(request.CompanyCountry);
        request.CompanyCity = Clean(request.CompanyCity);
        request.LinkedInUrl = Clean(request.LinkedInUrl);
        request.InstagramUrl = Clean(request.InstagramUrl);
        request.FacebookUrl = Clean(request.FacebookUrl);
        request.YoutubeUrl = Clean(request.YoutubeUrl);
        request.TelegramUrl = Clean(request.TelegramUrl);
        request.TiktokUrl = Clean(request.TiktokUrl);
        request.Benefits = (request.Benefits ?? [])
            .Select(Clean)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string Validate(SaveCompanyProfileRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName))
            return "Company name boş ola bilməz.";

        if ((request.Benefits?.Count ?? 0) > 12)
            return "Ən çox 12 benefit əlavə etmək olar.";

        if (request.Benefits?.Any(item => item.Length > 70) == true)
            return "Benefit adı 70 simvoldan uzun ola bilməz.";

        var urls = new[]
        {
            request.Website,
            request.CompanyVideo,
            request.LinkedInUrl,
            request.InstagramUrl,
            request.FacebookUrl,
            request.YoutubeUrl,
            request.TelegramUrl,
            request.TiktokUrl
        };

        if (urls.Any(item =>
            !string.IsNullOrWhiteSpace(item)
            && (!Uri.TryCreate(item, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp
                    && uri.Scheme != Uri.UriSchemeHttps))))
        {
            return "Website və social media linkləri düzgün URL olmalıdır.";
        }

        return string.Empty;
    }

    private static void Apply(
        SaveCompanyProfileRequest request,
        CompanyProfile profile)
    {
        profile.CompanyName = request.CompanyName;
        profile.CompanyType = Clean(request.CompanyType);
        profile.ActivityScope = Clean(request.ActivityScope);
        profile.FoundationYear = request.FoundationYear;
        profile.EmployeeCount = Clean(request.EmployeeCount);
        profile.Website = Clean(request.Website);
        profile.PageLanguage = Clean(request.PageLanguage);
        profile.CompanyVideo = Clean(request.CompanyVideo);
        profile.CompanyDescription = Clean(request.CompanyDescription);
        profile.CompanyCulture = Clean(request.CompanyCulture);
        profile.WhyWorkWithUs = Clean(request.WhyWorkWithUs);
        profile.BenefitsJson = JsonSerializer.Serialize(
            request.Benefits ?? [],
            JsonOptions);
        profile.CompanyAddress = Clean(request.CompanyAddress);
        profile.CompanyCountry = Clean(request.CompanyCountry);
        profile.CompanyCity = Clean(request.CompanyCity);
        profile.LinkedInUrl = Clean(request.LinkedInUrl);
        profile.InstagramUrl = Clean(request.InstagramUrl);
        profile.FacebookUrl = Clean(request.FacebookUrl);
        profile.YoutubeUrl = Clean(request.YoutubeUrl);
        profile.TelegramUrl = Clean(request.TelegramUrl);
        profile.TiktokUrl = Clean(request.TiktokUrl);
    }

    private static CompanyProfileDto ToDto(CompanyProfile profile)
    {
        List<string> benefits;

        try
        {
            benefits = JsonSerializer.Deserialize<List<string>>(
                profile.BenefitsJson,
                JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            benefits = [];
        }

        return new CompanyProfileDto
        {
            CompanyName = profile.CompanyName,
            CompanyType = profile.CompanyType,
            ActivityScope = profile.ActivityScope,
            FoundationYear = profile.FoundationYear,
            EmployeeCount = profile.EmployeeCount,
            Website = profile.Website,
            PageLanguage = profile.PageLanguage,
            CompanyVideo = profile.CompanyVideo,
            CompanyDescription = profile.CompanyDescription,
            CompanyCulture = profile.CompanyCulture,
            WhyWorkWithUs = profile.WhyWorkWithUs,
            Benefits = benefits,
            CompanyAddress = profile.CompanyAddress,
            CompanyCountry = profile.CompanyCountry,
            CompanyCity = profile.CompanyCity,
            LinkedInUrl = profile.LinkedInUrl,
            InstagramUrl = profile.InstagramUrl,
            FacebookUrl = profile.FacebookUrl,
            YoutubeUrl = profile.YoutubeUrl,
            TelegramUrl = profile.TelegramUrl,
            TiktokUrl = profile.TiktokUrl,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    private static CompanyProfileResponse Successful(
        int companyOwnerUserId,
        CompanyProfileDto profile,
        string message)
    {
        return new CompanyProfileResponse
        {
            Success = true,
            Message = message,
            CompanyOwnerUserId = companyOwnerUserId,
            Profile = profile
        };
    }

    private static CompanyProfileResponse Forbidden()
    {
        return Failed(
            "Bu company profile-a giriş icazəniz yoxdur.",
            CompanyProfileErrorCodes.Forbidden);
    }

    private static CompanyProfileResponse Failed(
        string message,
        string errorCode)
    {
        return new CompanyProfileResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private static string? EmptyToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string BuildDisplayName(User owner)
    {
        var name = string.Join(
            " ",
            new[] { owner.Name, owner.Surname }
                .Where(item => !string.IsNullOrWhiteSpace(item)));

        return string.IsNullOrWhiteSpace(name)
            ? owner.Email
            : name;
    }
}
