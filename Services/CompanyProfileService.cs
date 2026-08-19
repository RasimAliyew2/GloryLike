using System.Text.Json;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.CompanyProfile;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class CompanyProfileService : ICompanyProfileService
{
    private const int MaximumLocationCount = 20;
    private const int MaximumLogoBytes = 350 * 1024;
    private const int MaximumLogoDataUrlLength = 500000;

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
            .AsSplitQuery()
            .Include(item => item.Locations)
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
            .AsSplitQuery()
            .Include(item => item.Locations)
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

        var locationSyncError = SynchronizeLocations(request, profile);

        if (!string.IsNullOrWhiteSpace(locationSyncError))
        {
            return Failed(
                locationSyncError,
                CompanyProfileErrorCodes.Validation);
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
        request.Website = NormalizeWebsite(request.Website);
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
        request.LogoDataUrl = Clean(request.LogoDataUrl);
        request.Locations = (request.Locations ?? [])
            .Where(item => item is not null)
            .Select(item => new CompanyLocationInput
            {
                Id = item.Id is > 0 ? item.Id : null,
                Name = Clean(item.Name),
                Address = Clean(item.Address),
                Country = Clean(item.Country),
                City = Clean(item.City)
            })
            .Where(item => HasLocationValue(item))
            .Take(MaximumLocationCount + 1)
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

        if (request.Website?.Length > 240)
            return "Website 240 simvoldan uzun ola bilməz.";

        if ((request.Locations?.Count ?? 0) > MaximumLocationCount)
            return $"Ən çox {MaximumLocationCount} company location əlavə etmək olar.";

        if (request.Locations?.Any(item =>
                Clean(item.Name).Length > 120
                || Clean(item.Address).Length > 240
                || Clean(item.Country).Length > 100
                || Clean(item.City).Length > 100) == true)
        {
            return "Location xanalarından biri icazə verilən uzunluğu keçir.";
        }

        var logoValidation = ValidateLogoDataUrl(request.LogoDataUrl);
        if (!string.IsNullOrWhiteSpace(logoValidation))
            return logoValidation;

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
        profile.Website = NormalizeWebsite(request.Website);
        profile.PageLanguage = Clean(request.PageLanguage);
        profile.CompanyVideo = Clean(request.CompanyVideo);
        profile.CompanyDescription = Clean(request.CompanyDescription);
        profile.CompanyCulture = Clean(request.CompanyCulture);
        profile.WhyWorkWithUs = Clean(request.WhyWorkWithUs);
        profile.BenefitsJson = JsonSerializer.Serialize(
            request.Benefits ?? [],
            JsonOptions);
        profile.LogoDataUrl = Clean(request.LogoDataUrl);
        var primaryLocation = request.Locations?.FirstOrDefault();
        profile.CompanyAddress = primaryLocation is null
            ? Clean(request.CompanyAddress)
            : Clean(primaryLocation.Address);
        profile.CompanyCountry = primaryLocation is null
            ? Clean(request.CompanyCountry)
            : Clean(primaryLocation.Country);
        profile.CompanyCity = primaryLocation is null
            ? Clean(request.CompanyCity)
            : Clean(primaryLocation.City);
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
            LogoDataUrl = profile.LogoDataUrl,
            Locations = BuildLocations(profile),
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

    private static string NormalizeWebsite(string? value)
    {
        var cleaned = Clean(value);
        if (string.IsNullOrWhiteSpace(cleaned))
            return string.Empty;

        if (cleaned.StartsWith("//", StringComparison.Ordinal))
            return $"https:{cleaned}";

        return cleaned.Contains("://", StringComparison.Ordinal)
            ? cleaned
            : $"https://{cleaned}";
    }

    private static bool HasLocationValue(CompanyLocationInput location)
    {
        return !string.IsNullOrWhiteSpace(location.Name)
            || !string.IsNullOrWhiteSpace(location.Address)
            || !string.IsNullOrWhiteSpace(location.Country)
            || !string.IsNullOrWhiteSpace(location.City);
    }

    private string SynchronizeLocations(
        SaveCompanyProfileRequest request,
        CompanyProfile profile)
    {
        var requestedLocations = request.Locations ?? [];
        var existingById = profile.Locations.ToDictionary(item => item.Id);
        var requestedIds = requestedLocations
            .Where(item => item.Id.HasValue)
            .Select(item => item.Id!.Value)
            .ToHashSet();

        if (requestedIds.Any(id => !existingById.ContainsKey(id)))
            return "Seçilən location bu company profile-a aid deyil.";

        foreach (var existing in profile.Locations
                     .Where(item => !requestedIds.Contains(item.Id))
                     .ToList())
        {
            _dbContext.CompanyLocations.Remove(existing);
        }

        for (var index = 0; index < requestedLocations.Count; index++)
        {
            var input = requestedLocations[index];
            CompanyLocation location;

            if (input.Id.HasValue)
            {
                location = existingById[input.Id.Value];
            }
            else
            {
                location = new CompanyLocation();
                profile.Locations.Add(location);
            }

            location.Name = Clean(input.Name);
            location.Address = Clean(input.Address);
            location.Country = Clean(input.Country);
            location.City = Clean(input.City);
            location.SortOrder = index;
        }

        return string.Empty;
    }

    private static List<CompanyLocationDto> BuildLocations(
        CompanyProfile profile)
    {
        var locations = profile.Locations
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .Select(item => new CompanyLocationDto
            {
                Id = item.Id,
                Name = item.Name,
                Address = item.Address,
                Country = item.Country,
                City = item.City,
                SortOrder = item.SortOrder,
                DisplayName = BuildLocationDisplayName(
                    item.Name,
                    item.Address,
                    item.City,
                    item.Country)
            })
            .ToList();

        if (locations.Count == 0
            && (!string.IsNullOrWhiteSpace(profile.CompanyAddress)
                || !string.IsNullOrWhiteSpace(profile.CompanyCountry)
                || !string.IsNullOrWhiteSpace(profile.CompanyCity)))
        {
            locations.Add(new CompanyLocationDto
            {
                Address = profile.CompanyAddress,
                Country = profile.CompanyCountry,
                City = profile.CompanyCity,
                DisplayName = BuildLocationDisplayName(
                    string.Empty,
                    profile.CompanyAddress,
                    profile.CompanyCity,
                    profile.CompanyCountry)
            });
        }

        return locations;
    }

    private static string BuildLocationDisplayName(
        string? name,
        string? address,
        string? city,
        string? country)
    {
        if (!string.IsNullOrWhiteSpace(name))
            return name.Trim();

        var parts = new[] { city, address, country }
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return string.Join(", ", parts);
    }

    private static string ValidateLogoDataUrl(string? value)
    {
        var dataUrl = Clean(value);
        if (string.IsNullOrWhiteSpace(dataUrl))
            return string.Empty;

        if (dataUrl.Length > MaximumLogoDataUrlLength)
            return "Logo maksimum 350 KB ola bilər.";

        var supportedPrefix = dataUrl.StartsWith(
                "data:image/jpeg;base64,",
                StringComparison.OrdinalIgnoreCase)
            || dataUrl.StartsWith(
                "data:image/png;base64,",
                StringComparison.OrdinalIgnoreCase);

        var separatorIndex = dataUrl.IndexOf(',');
        if (!supportedPrefix || separatorIndex < 0)
            return "Logo yalnız JPG və ya PNG formatında olmalıdır.";

        try
        {
            var bytes = Convert.FromBase64String(dataUrl[(separatorIndex + 1)..]);
            return bytes.Length <= MaximumLogoBytes
                ? string.Empty
                : "Logo maksimum 350 KB ola bilər.";
        }
        catch (FormatException)
        {
            return "Logo məlumatı düzgün Base64 şəkil deyil.";
        }
    }

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
