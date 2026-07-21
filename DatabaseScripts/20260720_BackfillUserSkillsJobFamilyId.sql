SET XACT_ABORT ON;
BEGIN TRANSACTION;

BEGIN TRY
    IF OBJECT_ID(N'dbo.UserSkills', N'U') IS NULL
        THROW 51000, 'dbo.UserSkills cədvəli tapılmadı.', 1;

    IF OBJECT_ID(N'dbo.JobFamilies', N'U') IS NULL
        THROW 51001, 'dbo.JobFamilies cədvəli tapılmadı.', 1;

    IF COL_LENGTH(N'dbo.UserSkills', N'JobFamilyId') IS NULL
        ALTER TABLE dbo.UserSkills ADD JobFamilyId INT NULL;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.JobFamilies AS jobFamily
        GROUP BY LOWER(LTRIM(RTRIM(jobFamily.JobName)))
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51002, 'JobFamilies daxilində eyni ada uyğun birdən çox ID var. Avtomatik backfill dayandırıldı.', 1;
    END;

    UPDATE userSkill
    SET
        userSkill.JobFamilyId = jobFamily.Id,
        userSkill.JobFamilyName = jobFamily.JobName
    FROM dbo.UserSkills AS userSkill
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON LOWER(LTRIM(RTRIM(userSkill.JobFamilyName))) =
           LOWER(LTRIM(RTRIM(jobFamily.JobName)))
    WHERE
        userSkill.JobFamilyId IS NULL
        OR userSkill.JobFamilyId <= 0
        OR NOT EXISTS
        (
            SELECT 1
            FROM dbo.JobFamilies AS currentJobFamily
            WHERE currentJobFamily.Id = userSkill.JobFamilyId
        );

    IF EXISTS
    (
        SELECT 1
        FROM dbo.UserSkills AS userSkill
        LEFT JOIN dbo.JobFamilies AS jobFamily
            ON jobFamily.Id = userSkill.JobFamilyId
        WHERE userSkill.JobFamilyId IS NULL OR jobFamily.Id IS NULL
    )
    BEGIN
        THROW 51003, 'Bəzi UserSkills sətirləri JobFamilyName əsasında uyğunlaşdırılmadı.', 1;
    END;

    UPDATE userSkill
    SET userSkill.JobFamilyName = jobFamily.JobName
    FROM dbo.UserSkills AS userSkill
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON jobFamily.Id = userSkill.JobFamilyId;

    DECLARE @defaultConstraintName SYSNAME;

    SELECT @defaultConstraintName = constraintInfo.name
    FROM sys.default_constraints AS constraintInfo
    INNER JOIN sys.columns AS columnInfo
        ON columnInfo.default_object_id = constraintInfo.object_id
    WHERE
        constraintInfo.parent_object_id = OBJECT_ID(N'dbo.UserSkills')
        AND columnInfo.name = N'JobFamilyId';

    IF @defaultConstraintName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.UserSkills DROP CONSTRAINT '
            + QUOTENAME(@defaultConstraintName) + N';');
    END;

    ALTER TABLE dbo.UserSkills ALTER COLUMN JobFamilyId INT NOT NULL;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE
            parent_object_id = OBJECT_ID(N'dbo.UserSkills')
            AND name = N'FK_UserSkills_JobFamilies_JobFamilyId'
    )
    BEGIN
        ALTER TABLE dbo.UserSkills WITH CHECK
        ADD CONSTRAINT FK_UserSkills_JobFamilies_JobFamilyId
            FOREIGN KEY (JobFamilyId) REFERENCES dbo.JobFamilies(Id);

        ALTER TABLE dbo.UserSkills
        CHECK CONSTRAINT FK_UserSkills_JobFamilies_JobFamilyId;
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE
            object_id = OBJECT_ID(N'dbo.UserSkills')
            AND name = N'IX_UserSkills_JobFamilyId'
    )
    BEGIN
        CREATE INDEX IX_UserSkills_JobFamilyId
            ON dbo.UserSkills(JobFamilyId);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
