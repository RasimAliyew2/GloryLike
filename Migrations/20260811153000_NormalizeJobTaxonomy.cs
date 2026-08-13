using GloryLikeBackend.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GloryLikeBackend.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260811153000_NormalizeJobTaxonomy")]
public partial class NormalizeJobTaxonomy : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            SET NOCOUNT ON;
            SET XACT_ABORT ON;
            
            -- Production-safe in-place conversion:
            -- JobFamilies -> Seniorities -> Positions -> Skills
            -- becomes
            -- JobFamilies -> Positions -> PositionSeniorities -> Seniorities
            --                         \-> Skills
            --
            -- Skills stay attached to Position. The API projects the same Position skills
            -- under every linked Seniority, so one missing/extra level can never create a
            -- different skill list.
            
            IF OBJECT_ID(N'dbo.PositionSeniorities', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.Positions', N'JobFamilyId') IS NOT NULL
               AND COL_LENGTH(N'dbo.Positions', N'SeniorityId') IS NULL
               AND COL_LENGTH(N'dbo.Seniorities', N'JobFamilyId') IS NULL
            BEGIN
                PRINT N'Job taxonomy is already normalized. No changes were made.';
                RETURN;
            END;
            
            BEGIN TRY
                BEGIN TRANSACTION;
            
                IF OBJECT_ID(N'dbo.JobFamilies', N'U') IS NULL
                   OR OBJECT_ID(N'dbo.Seniorities', N'U') IS NULL
                   OR OBJECT_ID(N'dbo.Positions', N'U') IS NULL
                   OR OBJECT_ID(N'dbo.Skills', N'U') IS NULL
                BEGIN
                    THROW 51000, 'Required legacy taxonomy tables were not found.', 1;
                END;
            
                IF COL_LENGTH(N'dbo.Seniorities', N'JobFamilyId') IS NULL
                   OR COL_LENGTH(N'dbo.Positions', N'SeniorityId') IS NULL
                   OR COL_LENGTH(N'dbo.Skills', N'PositionId') IS NULL
                BEGIN
                    THROW 51001, 'Legacy taxonomy columns do not match the expected schema.', 1;
                END;
            
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Seniorities
                    WHERE NULLIF(LTRIM(RTRIM([Name])), N'') IS NULL
                       OR LTRIM(RTRIM([Name])) NOT IN
                          (N'Junior', N'Middle', N'Senior', N'Lead', N'Head')
                )
                BEGIN
                    THROW 51002, 'Seniorities contains an empty or unsupported level name.', 1;
                END;
            
                IF
                (
                    SELECT COUNT(DISTINCT LTRIM(RTRIM([Name])))
                    FROM dbo.Seniorities
                ) <> 5
                BEGIN
                    THROW 51003, 'Expected exactly five unique seniority names.', 1;
                END;
            
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Positions AS position
                    LEFT JOIN dbo.Seniorities AS seniority
                        ON seniority.Id = position.SeniorityId
                    WHERE seniority.Id IS NULL
                )
                BEGIN
                    THROW 51004, 'A Position references a missing Seniority.', 1;
                END;
            
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Skills AS skill
                    LEFT JOIN dbo.Positions AS position
                        ON position.Id = skill.PositionId
                    WHERE position.Id IS NULL
                )
                BEGIN
                    THROW 51005, 'A Skill references a missing Position.', 1;
                END;
            
                DECLARE @dropForeignKeys NVARCHAR(MAX) = N'';
            
                SELECT @dropForeignKeys = @dropForeignKeys
                    + N'ALTER TABLE '
                    + QUOTENAME(OBJECT_SCHEMA_NAME(parent_object_id))
                    + N'.'
                    + QUOTENAME(OBJECT_NAME(parent_object_id))
                    + N' DROP CONSTRAINT '
                    + QUOTENAME([name])
                    + N';'
                FROM sys.foreign_keys
                WHERE referenced_object_id IN
                      (
                          OBJECT_ID(N'dbo.Seniorities'),
                          OBJECT_ID(N'dbo.Positions'),
                          OBJECT_ID(N'dbo.Skills')
                      )
                   OR parent_object_id IN
                      (
                          OBJECT_ID(N'dbo.Seniorities'),
                          OBJECT_ID(N'dbo.Positions'),
                          OBJECT_ID(N'dbo.Skills')
                      );
            
                IF @dropForeignKeys <> N''
                    EXEC sys.sp_executesql @dropForeignKeys;
            
                IF COL_LENGTH(N'dbo.Seniorities', N'SortOrder') IS NULL
                    ALTER TABLE dbo.Seniorities ADD SortOrder INT NULL;
            
                IF COL_LENGTH(N'dbo.Positions', N'JobFamilyId') IS NULL
                    ALTER TABLE dbo.Positions ADD JobFamilyId INT NULL;
            
                CREATE TABLE #SeniorityMap
                (
                    OldSeniorityId INT NOT NULL PRIMARY KEY,
                    NewSeniorityId INT NOT NULL
                );
            
                ;WITH CanonicalSeniorities AS
                (
                    SELECT
                        LTRIM(RTRIM([Name])) AS SeniorityName,
                        MIN(Id) AS NewSeniorityId
                    FROM dbo.Seniorities
                    GROUP BY LTRIM(RTRIM([Name]))
                )
                INSERT INTO #SeniorityMap
                (
                    OldSeniorityId,
                    NewSeniorityId
                )
                SELECT
                    seniority.Id,
                    canonical.NewSeniorityId
                FROM dbo.Seniorities AS seniority
                INNER JOIN CanonicalSeniorities AS canonical
                    ON canonical.SeniorityName = LTRIM(RTRIM(seniority.[Name]));
            
                CREATE TABLE #PositionMap
                (
                    OldPositionId INT NOT NULL PRIMARY KEY,
                    NewPositionId INT NOT NULL,
                    JobFamilyId INT NOT NULL
                );
            
                ;WITH CanonicalPositions AS
                (
                    SELECT
                        seniority.JobFamilyId,
                        LTRIM(RTRIM(position.[Name])) AS PositionName,
                        MIN(position.Id) AS NewPositionId
                    FROM dbo.Positions AS position
                    INNER JOIN dbo.Seniorities AS seniority
                        ON seniority.Id = position.SeniorityId
                    GROUP BY
                        seniority.JobFamilyId,
                        LTRIM(RTRIM(position.[Name]))
                )
                INSERT INTO #PositionMap
                (
                    OldPositionId,
                    NewPositionId,
                    JobFamilyId
                )
                SELECT
                    position.Id,
                    canonical.NewPositionId,
                    seniority.JobFamilyId
                FROM dbo.Positions AS position
                INNER JOIN dbo.Seniorities AS seniority
                    ON seniority.Id = position.SeniorityId
                INNER JOIN CanonicalPositions AS canonical
                    ON canonical.JobFamilyId = seniority.JobFamilyId
                   AND canonical.PositionName = LTRIM(RTRIM(position.[Name]));
            
                CREATE TABLE #SkillMap
                (
                    OldSkillId INT NOT NULL PRIMARY KEY,
                    NewSkillId INT NOT NULL,
                    NewPositionId INT NOT NULL
                );
            
                ;WITH CanonicalSkills AS
                (
                    SELECT
                        positionMap.NewPositionId,
                        LTRIM(RTRIM(skill.SkillName)) AS SkillName,
                        MIN(skill.Id) AS NewSkillId
                    FROM dbo.Skills AS skill
                    INNER JOIN #PositionMap AS positionMap
                        ON positionMap.OldPositionId = skill.PositionId
                    GROUP BY
                        positionMap.NewPositionId,
                        LTRIM(RTRIM(skill.SkillName))
                )
                INSERT INTO #SkillMap
                (
                    OldSkillId,
                    NewSkillId,
                    NewPositionId
                )
                SELECT
                    skill.Id,
                    canonical.NewSkillId,
                    positionMap.NewPositionId
                FROM dbo.Skills AS skill
                INNER JOIN #PositionMap AS positionMap
                    ON positionMap.OldPositionId = skill.PositionId
                INNER JOIN CanonicalSkills AS canonical
                    ON canonical.NewPositionId = positionMap.NewPositionId
                   AND canonical.SkillName = LTRIM(RTRIM(skill.SkillName));
            
                UPDATE position
                SET position.JobFamilyId = positionMap.JobFamilyId
                FROM dbo.Positions AS position
                INNER JOIN #PositionMap AS positionMap
                    ON positionMap.OldPositionId = position.Id;
            
                IF OBJECT_ID(N'dbo.Vacancies', N'U') IS NOT NULL
                BEGIN
                    UPDATE vacancy
                    SET
                        vacancy.PositionId = positionMap.NewPositionId,
                        vacancy.SeniorityId = seniorityMap.NewSeniorityId
                    FROM dbo.Vacancies AS vacancy
                    INNER JOIN #PositionMap AS positionMap
                        ON positionMap.OldPositionId = vacancy.PositionId
                    INNER JOIN #SeniorityMap AS seniorityMap
                        ON seniorityMap.OldSeniorityId = vacancy.SeniorityId;
                END;
            
                IF OBJECT_ID(N'dbo.UserSkills', N'U') IS NOT NULL
                BEGIN
                    EXEC sys.sp_executesql N'
                        UPDATE userSkill
                        SET
                            userSkill.SkillId = COALESCE(skillMap.NewSkillId, userSkill.SkillId),
                            userSkill.PositionId = COALESCE(positionMap.NewPositionId, userSkill.PositionId),
                            userSkill.SeniorityId = COALESCE(seniorityMap.NewSeniorityId, userSkill.SeniorityId)
                        FROM dbo.UserSkills AS userSkill
                        LEFT JOIN #SkillMap AS skillMap
                            ON skillMap.OldSkillId = userSkill.SkillId
                        LEFT JOIN #PositionMap AS positionMap
                            ON positionMap.OldPositionId = userSkill.PositionId
                        LEFT JOIN #SeniorityMap AS seniorityMap
                            ON seniorityMap.OldSeniorityId = userSkill.SeniorityId;';
                END;
            
                IF OBJECT_ID(N'dbo.SkillQuestionnaires', N'U') IS NOT NULL
                   AND COL_LENGTH(N'dbo.SkillQuestionnaires', N'SkillId') IS NOT NULL
                BEGIN
                    EXEC sys.sp_executesql N'
                        UPDATE questionnaire
                        SET questionnaire.SkillId = skillMap.NewSkillId
                        FROM dbo.SkillQuestionnaires AS questionnaire
                        INNER JOIN #SkillMap AS skillMap
                            ON skillMap.OldSkillId = questionnaire.SkillId;';
                END;
            
                IF OBJECT_ID(N'dbo.VacancySkillRequirements', N'U') IS NOT NULL
                BEGIN
                    IF EXISTS
                    (
                        SELECT 1
                        FROM sys.indexes
                        WHERE object_id = OBJECT_ID(N'dbo.VacancySkillRequirements')
                          AND [name] = N'UX_VacancySkillRequirements_VacancyId_SkillId'
                    )
                    BEGIN
                        DROP INDEX UX_VacancySkillRequirements_VacancyId_SkillId
                            ON dbo.VacancySkillRequirements;
                    END;
            
                    ;WITH RankedRequirements AS
                    (
                        SELECT
                            requirement.Id,
                            ROW_NUMBER() OVER
                            (
                                PARTITION BY
                                    requirement.VacancyId,
                                    skillMap.NewSkillId
                                ORDER BY requirement.Id
                            ) AS DuplicateNumber
                        FROM dbo.VacancySkillRequirements AS requirement
                        INNER JOIN #SkillMap AS skillMap
                            ON skillMap.OldSkillId = requirement.SkillId
                    )
                    DELETE requirement
                    FROM dbo.VacancySkillRequirements AS requirement
                    INNER JOIN RankedRequirements AS ranked
                        ON ranked.Id = requirement.Id
                    WHERE ranked.DuplicateNumber > 1;
            
                    UPDATE requirement
                    SET requirement.SkillId = skillMap.NewSkillId
                    FROM dbo.VacancySkillRequirements AS requirement
                    INNER JOIN #SkillMap AS skillMap
                        ON skillMap.OldSkillId = requirement.SkillId;
                END;
            
                UPDATE skill
                SET
                    skill.PositionId = skillMap.NewPositionId,
                    skill.SkillName = LTRIM(RTRIM(skill.SkillName))
                FROM dbo.Skills AS skill
                INNER JOIN #SkillMap AS skillMap
                    ON skillMap.OldSkillId = skill.Id;
            
                DELETE skill
                FROM dbo.Skills AS skill
                INNER JOIN #SkillMap AS skillMap
                    ON skillMap.OldSkillId = skill.Id
                WHERE skillMap.OldSkillId <> skillMap.NewSkillId;
            
                DELETE position
                FROM dbo.Positions AS position
                INNER JOIN #PositionMap AS positionMap
                    ON positionMap.OldPositionId = position.Id
                WHERE positionMap.OldPositionId <> positionMap.NewPositionId;
            
                UPDATE position
                SET position.[Name] = LTRIM(RTRIM(position.[Name]))
                FROM dbo.Positions AS position;
            
                DELETE seniority
                FROM dbo.Seniorities AS seniority
                INNER JOIN #SeniorityMap AS seniorityMap
                    ON seniorityMap.OldSeniorityId = seniority.Id
                WHERE seniorityMap.OldSeniorityId <> seniorityMap.NewSeniorityId;
            
                UPDATE seniority
                SET
                    seniority.[Name] = LTRIM(RTRIM(seniority.[Name])),
                    seniority.SortOrder = CASE LTRIM(RTRIM(seniority.[Name]))
                        WHEN N'Junior' THEN 1
                        WHEN N'Middle' THEN 2
                        WHEN N'Senior' THEN 3
                        WHEN N'Lead' THEN 4
                        WHEN N'Head' THEN 5
                    END
                FROM dbo.Seniorities AS seniority;
            
                IF EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.Positions')
                      AND [name] = N'IX_Positions_SeniorityId'
                )
                    DROP INDEX IX_Positions_SeniorityId ON dbo.Positions;
            
                IF EXISTS
                (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.Seniorities')
                      AND [name] = N'IX_Seniorities_JobFamilyId'
                )
                    DROP INDEX IX_Seniorities_JobFamilyId ON dbo.Seniorities;
            
                ALTER TABLE dbo.JobFamilies ALTER COLUMN JobName NVARCHAR(150) NOT NULL;
                ALTER TABLE dbo.Positions ALTER COLUMN [Name] NVARCHAR(150) NOT NULL;
                ALTER TABLE dbo.Positions ALTER COLUMN JobFamilyId INT NOT NULL;
                ALTER TABLE dbo.Positions DROP COLUMN SeniorityId;
                ALTER TABLE dbo.Seniorities ALTER COLUMN [Name] NVARCHAR(50) NOT NULL;
                ALTER TABLE dbo.Seniorities ALTER COLUMN SortOrder INT NOT NULL;
                ALTER TABLE dbo.Seniorities DROP COLUMN JobFamilyId;
                ALTER TABLE dbo.Skills ALTER COLUMN SkillName NVARCHAR(150) NOT NULL;
            
                CREATE TABLE dbo.PositionSeniorities
                (
                    PositionId INT NOT NULL,
                    SeniorityId INT NOT NULL,
                    CONSTRAINT PK_PositionSeniorities
                        PRIMARY KEY (PositionId, SeniorityId)
                );
            
                INSERT INTO dbo.PositionSeniorities
                (
                    PositionId,
                    SeniorityId
                )
                SELECT
                    position.Id,
                    seniority.Id
                FROM dbo.Positions AS position
                CROSS JOIN dbo.Seniorities AS seniority;
            
                CREATE UNIQUE INDEX UX_JobFamilies_JobName
                    ON dbo.JobFamilies(JobName);
            
                CREATE INDEX IX_Positions_JobFamilyId
                    ON dbo.Positions(JobFamilyId);
            
                CREATE UNIQUE INDEX UX_Positions_JobFamilyId_Name
                    ON dbo.Positions(JobFamilyId, [Name]);
            
                CREATE UNIQUE INDEX UX_Seniorities_Name
                    ON dbo.Seniorities([Name]);
            
                CREATE UNIQUE INDEX UX_Seniorities_SortOrder
                    ON dbo.Seniorities(SortOrder);
            
                CREATE INDEX IX_PositionSeniorities_SeniorityId
                    ON dbo.PositionSeniorities(SeniorityId);
            
                CREATE UNIQUE INDEX UX_Skills_PositionId_SkillName
                    ON dbo.Skills(PositionId, SkillName);
            
                ALTER TABLE dbo.Positions
                    ADD CONSTRAINT FK_Positions_JobFamilies_JobFamilyId
                    FOREIGN KEY (JobFamilyId)
                    REFERENCES dbo.JobFamilies(Id);
            
                ALTER TABLE dbo.Skills
                    ADD CONSTRAINT FK_Skills_Positions_PositionId
                    FOREIGN KEY (PositionId)
                    REFERENCES dbo.Positions(Id)
                    ON DELETE CASCADE;
            
                ALTER TABLE dbo.PositionSeniorities
                    ADD CONSTRAINT FK_PositionSeniorities_Positions_PositionId
                    FOREIGN KEY (PositionId)
                    REFERENCES dbo.Positions(Id)
                    ON DELETE CASCADE;
            
                ALTER TABLE dbo.PositionSeniorities
                    ADD CONSTRAINT FK_PositionSeniorities_Seniorities_SeniorityId
                    FOREIGN KEY (SeniorityId)
                    REFERENCES dbo.Seniorities(Id)
                    ON DELETE CASCADE;
            
                IF OBJECT_ID(N'dbo.Vacancies', N'U') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.Vacancies
                        ADD CONSTRAINT FK_Vacancies_Positions_PositionId
                        FOREIGN KEY (PositionId)
                        REFERENCES dbo.Positions(Id);
            
                    ALTER TABLE dbo.Vacancies
                        ADD CONSTRAINT FK_Vacancies_Seniorities_SeniorityId
                        FOREIGN KEY (SeniorityId)
                        REFERENCES dbo.Seniorities(Id);
                END;
            
                IF OBJECT_ID(N'dbo.VacancySkillRequirements', N'U') IS NOT NULL
                BEGIN
                    CREATE UNIQUE INDEX UX_VacancySkillRequirements_VacancyId_SkillId
                        ON dbo.VacancySkillRequirements(VacancyId, SkillId);
            
                    ALTER TABLE dbo.VacancySkillRequirements
                        ADD CONSTRAINT FK_VacancySkillRequirements_Skills_SkillId
                        FOREIGN KEY (SkillId)
                        REFERENCES dbo.Skills(Id);
                END;
            
                IF (SELECT COUNT(*) FROM dbo.Seniorities) <> 5
                    THROW 51006, 'Post-check failed: Seniorities must contain five rows.', 1;
            
                IF EXISTS
                (
                    SELECT 1
                    FROM dbo.Positions AS position
                    CROSS APPLY
                    (
                        SELECT COUNT(*) AS SeniorityCount
                        FROM dbo.PositionSeniorities AS link
                        WHERE link.PositionId = position.Id
                    ) AS coverage
                    WHERE coverage.SeniorityCount <> 5
                )
                    THROW 51007, 'Post-check failed: every Position must have five Seniorities.', 1;
            
                COMMIT TRANSACTION;
            
                SELECT N'Seniorities' AS EntityName, COUNT(*) AS RowCount
                FROM dbo.Seniorities
                UNION ALL
                SELECT N'Positions', COUNT(*)
                FROM dbo.Positions
                UNION ALL
                SELECT N'PositionSeniorities', COUNT(*)
                FROM dbo.PositionSeniorities
                UNION ALL
                SELECT N'Skills', COUNT(*)
                FROM dbo.Skills;
            END TRY
            BEGIN CATCH
                IF XACT_STATE() <> 0
                    ROLLBACK TRANSACTION;
            
                THROW;
            END CATCH;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "The taxonomy normalization merges duplicate production rows "
            + "and cannot be reversed without a database backup.");
    }
}
