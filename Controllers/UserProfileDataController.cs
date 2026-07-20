using System.Data;
using System.Data.Common;
using GloryLikeBackend.Data;
using GloryLikeBackend.Dtos.ProfileData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GloryLikeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserProfileDataController : ControllerBase
{
    private readonly AppDbContext _dbContext;

    public UserProfileDataController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("{userId:int}")]
    public async Task<ActionResult<UserProfileDataResponse>> Get(
        int userId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
            return BadRequest(Failed(userId, "UserId düzgün deyil."));

        var connection = _dbContext.Database.GetDbConnection();
        await OpenIfNeededAsync(connection, cancellationToken);

        var exists = await UserExistsAsync(connection, null, userId, cancellationToken);
        if (!exists)
            return NotFound(Failed(userId, "İstifadəçi tapılmadı."));

        var result = await ReadProfileDataAsync(connection, null, userId, cancellationToken);
        result.Success = true;
        result.Message = "Profile data yükləndi.";
        return Ok(result);
    }

    [HttpPost("save")]
    public async Task<ActionResult<UserProfileDataResponse>> Save(
        [FromBody] SaveUserProfileDataRequest request,
        CancellationToken cancellationToken)
    {
        if (request.UserId <= 0)
            return BadRequest(Failed(request.UserId, "UserId düzgün deyil."));

        var connection = _dbContext.Database.GetDbConnection();
        await OpenIfNeededAsync(connection, cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var exists = await UserExistsAsync(connection, transaction, request.UserId, cancellationToken);
            if (!exists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return NotFound(Failed(request.UserId, "İstifadəçi tapılmadı."));
            }

            await ExecuteAsync(
                connection,
                transaction,
                "DELETE FROM dbo.UserSkills WHERE UserId = @UserId;",
                cancellationToken,
                ("@UserId", request.UserId));

            await ExecuteAsync(
                connection,
                transaction,
                "DELETE FROM dbo.UserWorkExperiences WHERE UserId = @UserId;",
                cancellationToken,
                ("@UserId", request.UserId));

            foreach (var skill in request.Skills.Where(x => !string.IsNullOrWhiteSpace(x.SkillName)))
            {
                await ExecuteAsync(
                    connection,
                    transaction,
                    @"INSERT INTO dbo.UserSkills
                    (
                        UserId, SkillId, SkillName,
                        PositionId, PositionName,
                        SeniorityId, SeniorityName,
                        JobFamilyId, JobFamilyName,
                        SkillComplexity, Status, IsVerified,
                        KnowledgeScore, ExperienceScore, DepthScore, CredibilityScore,
                        TaskComplexity, OwnershipLevel, DepthTier,
                        ContextScore, ComplexityScore, OwnershipScore, ResultScore,
                        CreatedAt, UpdatedAt
                    )
                    VALUES
                    (
                        @UserId, @SkillId, @SkillName,
                        @PositionId, @PositionName,
                        @SeniorityId, @SeniorityName,
                        @JobFamilyId, @JobFamilyName,
                        @SkillComplexity, @Status, @IsVerified,
                        @KnowledgeScore, @ExperienceScore, @DepthScore, @CredibilityScore,
                        @TaskComplexity, @OwnershipLevel, @DepthTier,
                        @ContextScore, @ComplexityScore, @OwnershipScore, @ResultScore,
                        SYSUTCDATETIME(), SYSUTCDATETIME()
                    );",
                    cancellationToken,
                    ("@UserId", request.UserId),
                    ("@SkillId", skill.SkillId),
                    ("@SkillName", Normalize(skill.SkillName, 150)),
                    ("@PositionId", skill.PositionId),
                    ("@PositionName", Normalize(skill.PositionName, 150)),
                    ("@SeniorityId", skill.SeniorityId),
                    ("@SeniorityName", Normalize(skill.SeniorityName, 50)),
                    ("@JobFamilyId", skill.JobFamilyId),
                    ("@JobFamilyName", Normalize(skill.JobFamilyName, 150)),
                    ("@SkillComplexity", Normalize(skill.SkillComplexity, 30, "medium")),
                    ("@Status", Normalize(skill.Status, 30, "self_declared")),
                    ("@IsVerified", skill.IsVerified),
                    ("@KnowledgeScore", ClampScore(skill.KnowledgeScore)),
                    ("@ExperienceScore", ClampScore(skill.ExperienceScore)),
                    ("@DepthScore", ClampScore(skill.DepthScore)),
                    ("@CredibilityScore", ClampScore(skill.CredibilityScore)),
                    ("@TaskComplexity", Normalize(skill.TaskComplexity, 50)),
                    ("@OwnershipLevel", Normalize(skill.OwnershipLevel, 50)),
                    ("@DepthTier", Normalize(skill.DepthTier, 50)),
                    ("@ContextScore", ClampScore(skill.ContextScore)),
                    ("@ComplexityScore", ClampScore(skill.ComplexityScore)),
                    ("@OwnershipScore", ClampScore(skill.OwnershipScore)),
                    ("@ResultScore", ClampScore(skill.ResultScore)));
            }

            foreach (var experience in request.Experiences.Where(x => !string.IsNullOrWhiteSpace(x.CompanyName)))
            {
                await ExecuteAsync(
                    connection,
                    transaction,
                    @"INSERT INTO dbo.UserWorkExperiences
                    (
                        UserId, CompanyName, PositionName, StartYear, EndYear, FileName, CreatedAt, UpdatedAt
                    )
                    VALUES
                    (
                        @UserId, @CompanyName, @PositionName, @StartYear, @EndYear, @FileName, SYSUTCDATETIME(), SYSUTCDATETIME()
                    );",
                    cancellationToken,
                    ("@UserId", request.UserId),
                    ("@CompanyName", Normalize(experience.CompanyName, 150)),
                    ("@PositionName", Normalize(experience.PositionName, 150)),
                    ("@StartYear", Normalize(experience.StartYear, 30)),
                    ("@EndYear", Normalize(experience.EndYear, 30)),
                    ("@FileName", Normalize(experience.FileName, 260)));
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var result = await ReadProfileDataAsync(connection, null, request.UserId, cancellationToken);
        result.Success = true;
        result.Message = "Profile skills və experience SQL-də saxlandı.";
        return Ok(result);
    }

    private static async Task OpenIfNeededAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
    }

    private static async Task<bool> UserExistsAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int userId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT COUNT(1) FROM dbo.Users WHERE Id = @UserId;";
        AddParameter(command, "@UserId", userId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<UserProfileDataResponse> ReadProfileDataAsync(
        DbConnection connection,
        DbTransaction? transaction,
        int userId,
        CancellationToken cancellationToken)
    {
        var response = new UserProfileDataResponse
        {
            UserId = userId
        };

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"SELECT
                    SkillId, SkillName,
                    PositionId, PositionName,
                    SeniorityId, SeniorityName,
                    JobFamilyId, JobFamilyName,
                    SkillComplexity, Status, IsVerified,
                    KnowledgeScore, ExperienceScore, DepthScore, CredibilityScore,
                    TaskComplexity, OwnershipLevel, DepthTier,
                    ContextScore, ComplexityScore, OwnershipScore, ResultScore
                FROM dbo.UserSkills
                WHERE UserId = @UserId
                ORDER BY SkillName;";
            AddParameter(command, "@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                response.Skills.Add(new UserSkillProfileDto
                {
                    SkillId = GetInt(reader, "SkillId"),
                    SkillName = GetString(reader, "SkillName"),
                    PositionId = GetInt(reader, "PositionId"),
                    PositionName = GetString(reader, "PositionName"),
                    SeniorityId = GetInt(reader, "SeniorityId"),
                    SeniorityName = GetString(reader, "SeniorityName"),
                    JobFamilyId = GetInt(reader, "JobFamilyId"),
                    JobFamilyName = GetString(reader, "JobFamilyName"),
                    SkillComplexity = GetString(reader, "SkillComplexity"),
                    Status = GetString(reader, "Status"),
                    IsVerified = GetBool(reader, "IsVerified"),
                    KnowledgeScore = GetDouble(reader, "KnowledgeScore"),
                    ExperienceScore = GetDouble(reader, "ExperienceScore"),
                    DepthScore = GetDouble(reader, "DepthScore"),
                    CredibilityScore = GetDouble(reader, "CredibilityScore"),
                    TaskComplexity = GetString(reader, "TaskComplexity"),
                    OwnershipLevel = GetString(reader, "OwnershipLevel"),
                    DepthTier = GetString(reader, "DepthTier"),
                    ContextScore = GetDouble(reader, "ContextScore"),
                    ComplexityScore = GetDouble(reader, "ComplexityScore"),
                    OwnershipScore = GetDouble(reader, "OwnershipScore"),
                    ResultScore = GetDouble(reader, "ResultScore")
                });
            }
        }

        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = @"SELECT CompanyName, PositionName, StartYear, EndYear, FileName
                FROM dbo.UserWorkExperiences
                WHERE UserId = @UserId
                ORDER BY StartYear DESC, CompanyName;";
            AddParameter(command, "@UserId", userId);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                response.Experiences.Add(new UserWorkExperienceProfileDto
                {
                    CompanyName = GetString(reader, "CompanyName"),
                    PositionName = GetString(reader, "PositionName"),
                    StartYear = GetString(reader, "StartYear"),
                    EndYear = GetString(reader, "EndYear"),
                    FileName = GetString(reader, "FileName")
                });
            }
        }

        return response;
    }

    private static async Task ExecuteAsync(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;

        foreach (var parameter in parameters)
            AddParameter(command, parameter.Name, parameter.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string Normalize(string? value, int maxLength, string fallback = "")
    {
        var result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return result.Length <= maxLength ? result : result[..maxLength];
    }

    private static double ClampScore(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0;

        return Math.Clamp(value, 0, 100);
    }

    private static int GetInt(DbDataReader reader, string name)
    {
        var value = reader[name];
        return value == DBNull.Value ? 0 : Convert.ToInt32(value);
    }

    private static double GetDouble(DbDataReader reader, string name)
    {
        var value = reader[name];
        return value == DBNull.Value ? 0d : Convert.ToDouble(value);
    }

    private static string GetString(DbDataReader reader, string name)
    {
        var value = reader[name];
        return value == DBNull.Value ? string.Empty : Convert.ToString(value) ?? string.Empty;
    }

    private static bool GetBool(DbDataReader reader, string name)
    {
        var value = reader[name];
        return value != DBNull.Value && Convert.ToBoolean(value);
    }

    private static UserProfileDataResponse Failed(int userId, string message)
    {
        return new UserProfileDataResponse
        {
            Success = false,
            UserId = userId,
            Message = message
        };
    }
}
