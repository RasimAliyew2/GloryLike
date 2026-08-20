using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.ProfileData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/user-personal-profile")]
public sealed class UserPersonalProfileController : ControllerBase
{
    private const int MaxProfileImageDataUrlLength = 750_000;
    private readonly AppDbContext _dbContext;

    public UserPersonalProfileController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<UserPersonalProfileResponse>> Get(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
            return BadRequest(Failed(userId, "User ID düzgün deyil."));

        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        return user is null
            ? NotFound(Failed(userId, "İstifadəçi tapılmadı."))
            : Ok(ToResponse(user, "Profil yükləndi."));
    }

    [HttpPut("{userId:int}")]
    public async Task<ActionResult<UserPersonalProfileResponse>> Update(
        int userId,
        [FromBody] UpdateUserPersonalProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
            return BadRequest(Failed(userId, "User ID düzgün deyil."));

        var birthDate = request.BirthDate?.Date;
        var today = DateTime.UtcNow.Date;

        if (birthDate.HasValue
            && (birthDate.Value > today || birthDate.Value < today.AddYears(-120)))
        {
            return BadRequest(Failed(
                userId,
                "Doğum tarixi gələcək tarix ola bilməz və 120 ildən köhnə ola bilməz."));
        }

        var imageValidationError = ValidateProfileImage(request.ProfileImageDataUrl);
        if (!string.IsNullOrWhiteSpace(imageValidationError))
            return BadRequest(Failed(userId, imageValidationError));

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
            return NotFound(Failed(userId, "İstifadəçi tapılmadı."));

        user.Name = request.FirstName.Trim();
        user.Surname = request.LastName.Trim();
        user.BirthDate = birthDate;
        user.About = NormalizeOptional(request.About);
        user.ProfileImageDataUrl = NormalizeOptional(request.ProfileImageDataUrl);
        user.Age = birthDate.HasValue
            ? CalculateAge(birthDate.Value, today)
            : 0;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Ok(ToResponse(user, "Profil uğurla saxlanıldı."));
    }

    private static string ValidateProfileImage(string? dataUrl)
    {
        if (string.IsNullOrWhiteSpace(dataUrl))
            return string.Empty;

        if (dataUrl.Length > MaxProfileImageDataUrlLength)
            return "Profil şəkli 500 KB optimallaşdırılmış limitdən böyükdür.";

        var isAllowedType = dataUrl.StartsWith(
                "data:image/jpeg;base64,",
                StringComparison.OrdinalIgnoreCase)
            || dataUrl.StartsWith(
                "data:image/png;base64,",
                StringComparison.OrdinalIgnoreCase)
            || dataUrl.StartsWith(
                "data:image/webp;base64,",
                StringComparison.OrdinalIgnoreCase);

        if (!isAllowedType)
            return "Profil şəkli JPG, PNG və ya WEBP formatında olmalıdır.";

        var separatorIndex = dataUrl.IndexOf(',');
        if (separatorIndex < 0 || separatorIndex == dataUrl.Length - 1)
            return "Profil şəklinin data formatı düzgün deyil.";

        try
        {
            var bytes = Convert.FromBase64String(dataUrl[(separatorIndex + 1)..]);
            return bytes.Length <= 500 * 1024
                ? string.Empty
                : "Profil şəkli 500 KB optimallaşdırılmış limitdən böyükdür.";
        }
        catch (FormatException)
        {
            return "Profil şəklinin base64 formatı düzgün deyil.";
        }
    }

    private static int CalculateAge(DateTime birthDate, DateTime today)
    {
        var age = today.Year - birthDate.Year;
        return birthDate > today.AddYears(-age) ? age - 1 : age;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static UserPersonalProfileResponse ToResponse(
        GloryLikeBackend.Models.User user,
        string message)
    {
        return new UserPersonalProfileResponse
        {
            Success = true,
            Message = message,
            UserId = user.Id,
            FirstName = user.Name,
            LastName = user.Surname,
            BirthDate = user.BirthDate,
            About = user.About ?? string.Empty,
            ProfileImageDataUrl = user.ProfileImageDataUrl ?? string.Empty,
            Email = user.Email,
            AccountType = user.AccountType
        };
    }

    private static UserPersonalProfileResponse Failed(int userId, string message)
    {
        return new UserPersonalProfileResponse
        {
            Success = false,
            Message = message,
            UserId = userId
        };
    }
}
