using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260815120000_AddCandidateUserJobs")]
public partial class AddCandidateUserJobs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            SET NOCOUNT ON;
            SET XACT_ABORT ON;

            IF OBJECT_ID(N'dbo.UserJobs', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.UserJobs
                (
                    Id INT IDENTITY(1,1) NOT NULL,
                    UserId INT NOT NULL,
                    JobFamilyId INT NOT NULL,
                    JobFamilyName NVARCHAR(150) NOT NULL,
                    CreatedAt DATETIME2 NOT NULL
                        CONSTRAINT DF_UserJobs_CreatedAt DEFAULT SYSUTCDATETIME(),
                    UpdatedAt DATETIME2 NOT NULL
                        CONSTRAINT DF_UserJobs_UpdatedAt DEFAULT SYSUTCDATETIME(),
                    CONSTRAINT PK_UserJobs PRIMARY KEY (Id),
                    CONSTRAINT FK_UserJobs_Users_UserId
                        FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
                        ON DELETE CASCADE,
                    CONSTRAINT FK_UserJobs_JobFamilies_JobFamilyId
                        FOREIGN KEY (JobFamilyId) REFERENCES dbo.JobFamilies(Id)
                );

                CREATE UNIQUE INDEX UX_UserJobs_UserId
                    ON dbo.UserJobs(UserId);

                CREATE INDEX IX_UserJobs_JobFamilyId
                    ON dbo.UserJobs(JobFamilyId);
            END;

            IF OBJECT_ID(N'dbo.UserSkills', N'U') IS NOT NULL
            BEGIN
                ;WITH CandidateJobs AS
                (
                    SELECT
                        userSkill.UserId,
                        userSkill.JobFamilyId,
                        jobFamily.JobName AS JobFamilyName,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY userSkill.UserId
                            ORDER BY COUNT_BIG(*) DESC, userSkill.JobFamilyId
                        ) AS RowNumber
                    FROM dbo.UserSkills AS userSkill
                    INNER JOIN dbo.JobFamilies AS jobFamily
                        ON jobFamily.Id = userSkill.JobFamilyId
                    WHERE userSkill.JobFamilyId > 0
                    GROUP BY
                        userSkill.UserId,
                        userSkill.JobFamilyId,
                        jobFamily.JobName
                )
                INSERT INTO dbo.UserJobs
                (
                    UserId, JobFamilyId, JobFamilyName,
                    CreatedAt, UpdatedAt
                )
                SELECT
                    candidateJob.UserId,
                    candidateJob.JobFamilyId,
                    candidateJob.JobFamilyName,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                FROM CandidateJobs AS candidateJob
                WHERE candidateJob.RowNumber = 1
                  AND NOT EXISTS
                  (
                      SELECT 1
                      FROM dbo.UserJobs AS existing
                      WHERE existing.UserId = candidateJob.UserId
                  );
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'dbo.UserJobs', N'U') IS NOT NULL
                DROP TABLE dbo.UserJobs;
            """);
    }
}
