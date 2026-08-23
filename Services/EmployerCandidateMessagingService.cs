using System.Data;
using System.Data.Common;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.EmployerCandidates;
using GloryLikeBackend.Models;
using GloryLikeBackend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Services;

public sealed class EmployerCandidateMessagingService
    : IEmployerCandidateMessagingService
{
    private readonly AppDbContext _dbContext;
    private readonly ICompanyAccessService _companyAccessService;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EmployerCandidateMessagingService> _logger;

    public EmployerCandidateMessagingService(
        AppDbContext dbContext,
        ICompanyAccessService companyAccessService,
        TimeProvider timeProvider,
        ILogger<EmployerCandidateMessagingService> logger)
    {
        _dbContext = dbContext;
        _companyAccessService = companyAccessService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<EmployerCandidateProfileResponse> GetCandidateProfileAsync(
        int actorUserId,
        int candidateUserId,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId <= 0 || candidateUserId <= 0)
            return CandidateFailure("Employer və candidate ID düzgün deyil.", EmployerCandidateErrorCodes.Validation);

        var access = await _companyAccessService.ResolveAsync(
            actorUserId,
            cancellationToken);

        if (access is null)
            return CandidateFailure("Bu company candidate profilinə giriş icazəniz yoxdur.", EmployerCandidateErrorCodes.Forbidden);

        var candidate = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == candidateUserId
                    && user.AccountType == "candidate",
                cancellationToken);

        if (candidate is null)
            return CandidateFailure("Candidate tapılmadı.", EmployerCandidateErrorCodes.NotFound);

        if (!await CanAccessCandidateAsync(access.CompanyOwnerUserId, candidateUserId, cancellationToken))
            return CandidateFailure("Bu candidate sizin şirkətinizin Talent Radar və ya application məlumatlarına aid deyil.", EmployerCandidateErrorCodes.Forbidden);

        var job = await _dbContext.UserJobs
            .AsNoTracking()
            .Where(item => item.UserId == candidateUserId)
            .OrderByDescending(item => item.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var skillRows = await _dbContext.UserSkills
            .AsNoTracking()
            .Where(item => item.UserId == candidateUserId)
            .OrderByDescending(item => item.CredibilityScore)
            .ThenBy(item => item.SkillName)
            .ToListAsync(cancellationToken);

        var skills = skillRows
            .Select(item => new EmployerCandidateSkillDto
            {
                SkillId = item.SkillId,
                SkillName = item.SkillName,
                Status = item.Status,
                IsVerified = item.IsVerified,
                CredibilityScore = (int)Math.Floor(
                    Math.Clamp(item.CredibilityScore, 0d, 100d) + .5d)
            })
            .ToList();

        var history = await _dbContext.VacancyApplications
            .AsNoTracking()
            .Where(item =>
                item.CandidateUserId == candidateUserId
                && item.Vacancy.CompanyOwnerUserId == access.CompanyOwnerUserId)
            .OrderByDescending(item => item.AppliedAtUtc)
            .Select(item => new CandidateVacancyHistoryDto
            {
                VacancyId = item.VacancyId,
                PlatformVacancyId = item.Vacancy.PlatformVacancyId,
                RoleTitle = item.Vacancy.RoleTitle,
                JobFamilyName = item.Vacancy.JobFamilyName,
                PositionName = item.Vacancy.PositionName,
                LocationName = item.Vacancy.LocationName,
                ApplicationStatus = item.Status,
                AppliedAtUtc = item.AppliedAtUtc
            })
            .ToListAsync(cancellationToken);

        foreach (var item in history)
        {
            if (string.IsNullOrWhiteSpace(item.RoleTitle))
                item.RoleTitle = item.PositionName;
        }

        var currentJobName = job?.JobFamilyName ?? string.Empty;
        if (string.IsNullOrWhiteSpace(currentJobName))
        {
            currentJobName = await _dbContext.UserSkills
                .AsNoTracking()
                .Where(item => item.UserId == candidateUserId)
                .OrderByDescending(item => item.UpdatedAt)
                .Select(item => item.PositionName)
                .FirstOrDefaultAsync(cancellationToken)
                ?? string.Empty;
        }

        return new EmployerCandidateProfileResponse
        {
            Success = true,
            Message = "Candidate profili yükləndi.",
            Candidate = new EmployerCandidateProfileDto
            {
                UserId = candidate.Id,
                DisplayName = BuildDisplayName(candidate),
                UserName = candidate.UserName,
                Email = candidate.Email,
                BirthDate = candidate.BirthDate,
                About = candidate.About ?? string.Empty,
                ProfileImageDataUrl = candidate.ProfileImageDataUrl ?? string.Empty,
                CurrentJobName = currentJobName,
                Skills = skills,
                Experiences = await ReadExperiencesAsync(candidateUserId, cancellationToken),
                VacancyHistory = history,
                TeamMembers = await GetTeamMembersAsync(access, cancellationToken)
            }
        };
    }

    public async Task<CompanyMessagingOverviewResponse> GetOverviewAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(actorUserId, cancellationToken);
        if (access is null)
            return OverviewFailure("Company mesajlarına giriş icazəniz yoxdur.", EmployerCandidateErrorCodes.Forbidden);

        var messages = await _dbContext.CompanyCandidateMessages
            .AsNoTracking()
            .Include(item => item.Sender)
            .Include(item => item.Recipient)
            .Include(item => item.Candidate)
            .Where(item =>
                item.CompanyOwnerUserId == access.CompanyOwnerUserId
                && (item.SenderUserId == actorUserId
                    || item.RecipientUserId == actorUserId))
            .OrderByDescending(item => item.CreatedAtUtc)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var conversations = messages
            .GroupBy(item => new
            {
                OtherUserId = item.SenderUserId == actorUserId
                    ? item.RecipientUserId
                    : item.SenderUserId,
                item.CandidateUserId
            })
            .Select(group =>
            {
                var last = group.OrderByDescending(item => item.CreatedAtUtc).First();
                var other = last.SenderUserId == actorUserId
                    ? last.Recipient
                    : last.Sender;

                return new CompanyMessageConversationDto
                {
                    OtherUserId = group.Key.OtherUserId,
                    OtherDisplayName = BuildDisplayName(other),
                    CandidateUserId = group.Key.CandidateUserId,
                    CandidateDisplayName = BuildDisplayName(last.Candidate),
                    LastMessage = last.Body,
                    LastMessageAtUtc = last.CreatedAtUtc,
                    UnreadCount = group.Count(item =>
                        item.RecipientUserId == actorUserId
                        && !item.ReadAtUtc.HasValue)
                };
            })
            .OrderByDescending(item => item.LastMessageAtUtc)
            .ToList();

        return new CompanyMessagingOverviewResponse
        {
            Success = true,
            Message = "Company mesajları yükləndi.",
            UnreadCount = conversations.Sum(item => item.UnreadCount),
            TeamMembers = await GetTeamMembersAsync(access, cancellationToken),
            Conversations = conversations
        };
    }

    public async Task<CompanyUnreadCountResponse> GetUnreadCountAsync(
        int actorUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _companyAccessService.ResolveAsync(actorUserId, cancellationToken);
        if (access is null)
        {
            return new CompanyUnreadCountResponse
            {
                Success = false,
                Message = "Company mesajlarına giriş icazəniz yoxdur.",
                ErrorCode = EmployerCandidateErrorCodes.Forbidden
            };
        }

        var count = await _dbContext.CompanyCandidateMessages
            .AsNoTracking()
            .CountAsync(item =>
                item.CompanyOwnerUserId == access.CompanyOwnerUserId
                && item.RecipientUserId == actorUserId
                && !item.ReadAtUtc.HasValue,
                cancellationToken);

        return new CompanyUnreadCountResponse
        {
            Success = true,
            UnreadCount = count
        };
    }

    public async Task<CompanyMessageThreadResponse> GetThreadAsync(
        int actorUserId,
        int otherUserId,
        int candidateUserId,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateConversationAsync(
            actorUserId,
            otherUserId,
            candidateUserId,
            cancellationToken);

        if (!validation.Success || validation.Access is null)
            return ThreadFailure(validation.Message, validation.ErrorCode);

        var messages = await _dbContext.CompanyCandidateMessages
            .AsNoTracking()
            .Include(item => item.Sender)
            .Include(item => item.Recipient)
            .Include(item => item.Candidate)
            .Where(item =>
                item.CompanyOwnerUserId == validation.Access.CompanyOwnerUserId
                && item.CandidateUserId == candidateUserId
                && ((item.SenderUserId == actorUserId && item.RecipientUserId == otherUserId)
                    || (item.SenderUserId == otherUserId && item.RecipientUserId == actorUserId)))
            .OrderBy(item => item.CreatedAtUtc)
            .Take(1000)
            .ToListAsync(cancellationToken);

        return new CompanyMessageThreadResponse
        {
            Success = true,
            Messages = messages.Select(ToMessageDto).ToList()
        };
    }

    public async Task<CompanyMessageActionResponse> SendAsync(
        SendCompanyCandidateMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length == 0 || body.Length > 4000)
            return ActionFailure("Mesaj 1-4000 simvol arasında olmalıdır.", EmployerCandidateErrorCodes.Validation);

        var validation = await ValidateConversationAsync(
            request.ActorUserId,
            request.RecipientUserId,
            request.CandidateUserId,
            cancellationToken);

        if (!validation.Success || validation.Access is null)
            return ActionFailure(validation.Message, validation.ErrorCode);

        var entity = new CompanyCandidateMessage
        {
            CompanyOwnerUserId = validation.Access.CompanyOwnerUserId,
            SenderUserId = request.ActorUserId,
            RecipientUserId = request.RecipientUserId,
            CandidateUserId = request.CandidateUserId,
            Body = body,
            CreatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime
        };

        _dbContext.CompanyCandidateMessages.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.Entry(entity).Reference(item => item.Sender).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(item => item.Recipient).LoadAsync(cancellationToken);
        await _dbContext.Entry(entity).Reference(item => item.Candidate).LoadAsync(cancellationToken);

        return new CompanyMessageActionResponse
        {
            Success = true,
            Message = "Mesaj göndərildi.",
            Item = ToMessageDto(entity)
        };
    }

    public async Task<CompanyMessageActionResponse> MarkThreadReadAsync(
        MarkCompanyMessageThreadReadRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await ValidateConversationAsync(
            request.ActorUserId,
            request.OtherUserId,
            request.CandidateUserId,
            cancellationToken);

        if (!validation.Success || validation.Access is null)
            return ActionFailure(validation.Message, validation.ErrorCode);

        var unread = await _dbContext.CompanyCandidateMessages
            .Where(item =>
                item.CompanyOwnerUserId == validation.Access.CompanyOwnerUserId
                && item.CandidateUserId == request.CandidateUserId
                && item.SenderUserId == request.OtherUserId
                && item.RecipientUserId == request.ActorUserId
                && !item.ReadAtUtc.HasValue)
            .ToListAsync(cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var item in unread)
            item.ReadAtUtc = now;

        if (unread.Count > 0)
            await _dbContext.SaveChangesAsync(cancellationToken);

        return new CompanyMessageActionResponse
        {
            Success = true,
            Message = "Conversation oxunmuş kimi qeyd edildi."
        };
    }

    private async Task<ConversationValidation> ValidateConversationAsync(
        int actorUserId,
        int otherUserId,
        int candidateUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId <= 0 || otherUserId <= 0 || candidateUserId <= 0)
            return ConversationValidation.Fail("Mesaj məlumatları düzgün deyil.", EmployerCandidateErrorCodes.Validation);

        if (actorUserId == otherUserId)
            return ConversationValidation.Fail("Özünüzə mesaj göndərə bilməzsiniz.", EmployerCandidateErrorCodes.Conflict);

        var access = await _companyAccessService.ResolveAsync(actorUserId, cancellationToken);
        if (access is null)
            return ConversationValidation.Fail("Company mesajlarına giriş icazəniz yoxdur.", EmployerCandidateErrorCodes.Forbidden);

        var activeUserIds = await _companyAccessService.GetActiveUserIdsAsync(
            access.CompanyOwnerUserId,
            cancellationToken);

        if (!activeUserIds.Contains(otherUserId))
            return ConversationValidation.Fail("Seçilən istifadəçi sizin aktiv company team üzvünüz deyil.", EmployerCandidateErrorCodes.Forbidden);

        if (!await CanAccessCandidateAsync(access.CompanyOwnerUserId, candidateUserId, cancellationToken))
            return ConversationValidation.Fail("Bu candidate sizin şirkətinizə aid deyil.", EmployerCandidateErrorCodes.Forbidden);

        return ConversationValidation.Ok(access);
    }

    private async Task<bool> CanAccessCandidateAsync(
        int companyOwnerUserId,
        int candidateUserId,
        CancellationToken cancellationToken)
    {
        var hasApplication = await _dbContext.VacancyApplications
            .AsNoTracking()
            .AnyAsync(item =>
                item.CandidateUserId == candidateUserId
                && item.Vacancy.CompanyOwnerUserId == companyOwnerUserId,
                cancellationToken);

        if (hasApplication)
            return true;

        var hasCompanyConversation = await _dbContext.CompanyCandidateMessages
            .AsNoTracking()
            .AnyAsync(item =>
                item.CompanyOwnerUserId == companyOwnerUserId
                && item.CandidateUserId == candidateUserId,
                cancellationToken);

        if (hasCompanyConversation)
            return true;

        return await (
                from job in _dbContext.UserJobs.AsNoTracking()
                join vacancy in _dbContext.Vacancies.AsNoTracking()
                    on job.JobFamilyId equals vacancy.JobFamilyId
                where job.UserId == candidateUserId
                    && vacancy.CompanyOwnerUserId == companyOwnerUserId
                    && vacancy.Status == "Published"
                select job.UserId)
            .AnyAsync(cancellationToken);
    }

    private async Task<List<CompanyMessageTeamMemberDto>> GetTeamMembersAsync(
        CompanyAccessContext access,
        CancellationToken cancellationToken)
    {
        var activeUserIds = await _companyAccessService.GetActiveUserIdsAsync(
            access.CompanyOwnerUserId,
            cancellationToken);

        var users = await _dbContext.Users
            .AsNoTracking()
            .Where(item => activeUserIds.Contains(item.Id)
                && item.Id != access.ActorUserId)
            .ToListAsync(cancellationToken);

        var roleRows = await _dbContext.CompanyTeamInvitations
            .AsNoTracking()
            .Where(item =>
                item.OwnerUserId == access.CompanyOwnerUserId
                && item.Status == CompanyTeamInvitationStatuses.Active
                && item.AcceptedUserId.HasValue)
            .OrderByDescending(item => item.AcceptedAtUtc)
            .Select(item => new
            {
                UserId = item.AcceptedUserId!.Value,
                item.Role
            })
            .ToListAsync(cancellationToken);

        var roles = roleRows
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.First().Role);

        return users
            .Select(user => new CompanyMessageTeamMemberDto
            {
                UserId = user.Id,
                DisplayName = BuildDisplayName(user),
                Email = user.Email,
                Role = user.Id == access.CompanyOwnerUserId
                    ? "Admin"
                    : roles.GetValueOrDefault(user.Id, "Team member")
            })
            .OrderBy(item => item.DisplayName)
            .ToList();
    }

    private async Task<List<EmployerCandidateExperienceDto>> ReadExperiencesAsync(
        int candidateUserId,
        CancellationToken cancellationToken)
    {
        var result = new List<EmployerCandidateExperienceDto>();
        var connection = _dbContext.Database.GetDbConnection();
        var openedHere = connection.State != ConnectionState.Open;

        try
        {
            if (openedHere)
                await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT CompanyName, PositionName, StartYear, EndYear
                FROM dbo.UserWorkExperiences
                WHERE UserId = @UserId
                ORDER BY StartYear DESC, CompanyName;
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@UserId";
            parameter.Value = candidateUserId;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                result.Add(new EmployerCandidateExperienceDto
                {
                    CompanyName = ReadString(reader, "CompanyName"),
                    PositionName = ReadString(reader, "PositionName"),
                    StartYear = ReadString(reader, "StartYear"),
                    EndYear = ReadString(reader, "EndYear")
                });
            }
        }
        catch (DbException exception)
        {
            _logger.LogWarning(
                exception,
                "Candidate {CandidateUserId} experience məlumatı oxunmadı.",
                candidateUserId);
        }
        finally
        {
            if (openedHere && connection.State == ConnectionState.Open)
                await connection.CloseAsync();
        }

        return result;
    }

    private static string ReadString(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? string.Empty
            : Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
    }

    private static CompanyMessageDto ToMessageDto(CompanyCandidateMessage item)
    {
        return new CompanyMessageDto
        {
            Id = item.Id,
            SenderUserId = item.SenderUserId,
            SenderDisplayName = BuildDisplayName(item.Sender),
            RecipientUserId = item.RecipientUserId,
            RecipientDisplayName = BuildDisplayName(item.Recipient),
            CandidateUserId = item.CandidateUserId,
            CandidateDisplayName = BuildDisplayName(item.Candidate),
            Body = item.Body,
            CreatedAtUtc = item.CreatedAtUtc,
            ReadAtUtc = item.ReadAtUtc
        };
    }

    private static string BuildDisplayName(User user)
    {
        var value = string.Join(
            " ",
            new[] { user.Name, user.Surname }
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item.Trim()));

        if (!string.IsNullOrWhiteSpace(value))
            return value;

        if (!string.IsNullOrWhiteSpace(user.UserName))
            return user.UserName.Trim();

        return user.Email;
    }

    private static EmployerCandidateProfileResponse CandidateFailure(string message, string errorCode) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };

    private static CompanyMessagingOverviewResponse OverviewFailure(string message, string errorCode) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };

    private static CompanyMessageThreadResponse ThreadFailure(string message, string errorCode) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };

    private static CompanyMessageActionResponse ActionFailure(string message, string errorCode) =>
        new() { Success = false, Message = message, ErrorCode = errorCode };

    private sealed record ConversationValidation(
        bool Success,
        string Message,
        string ErrorCode,
        CompanyAccessContext? Access)
    {
        public static ConversationValidation Ok(CompanyAccessContext access) =>
            new(true, string.Empty, string.Empty, access);

        public static ConversationValidation Fail(string message, string errorCode) =>
            new(false, message, errorCode, null);
    }
}
