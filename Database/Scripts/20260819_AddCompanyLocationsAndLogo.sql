SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.CompanyProfiles', N'U') IS NULL
        THROW 51100, 'dbo.CompanyProfiles was not found.', 1;

    IF OBJECT_ID(N'dbo.Vacancies', N'U') IS NULL
        THROW 51101, 'dbo.Vacancies was not found.', 1;

    IF COL_LENGTH(N'dbo.CompanyProfiles', N'LogoDataUrl') IS NULL
    BEGIN
        ALTER TABLE dbo.CompanyProfiles
        ADD LogoDataUrl nvarchar(max) NOT NULL
            CONSTRAINT DF_CompanyProfiles_LogoDataUrl DEFAULT (N'');
    END;

    IF COL_LENGTH(N'dbo.Vacancies', N'CompanyLocationId') IS NULL
        ALTER TABLE dbo.Vacancies ADD CompanyLocationId int NULL;

    IF COL_LENGTH(N'dbo.Vacancies', N'LocationName') IS NULL
    BEGIN
        ALTER TABLE dbo.Vacancies
        ADD LocationName nvarchar(460) NOT NULL
            CONSTRAINT DF_Vacancies_LocationName DEFAULT (N'');
    END;

    IF OBJECT_ID(N'dbo.CompanyLocations', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CompanyLocations
        (
            Id int IDENTITY(1, 1) NOT NULL,
            CompanyProfileId int NOT NULL,
            [Name] nvarchar(120) NOT NULL,
            [Address] nvarchar(240) NOT NULL,
            Country nvarchar(100) NOT NULL,
            City nvarchar(100) NOT NULL,
            SortOrder int NOT NULL,
            CONSTRAINT PK_CompanyLocations PRIMARY KEY (Id),
            CONSTRAINT FK_CompanyLocations_CompanyProfiles_CompanyProfileId
                FOREIGN KEY (CompanyProfileId)
                REFERENCES dbo.CompanyProfiles(Id)
                ON DELETE CASCADE
        );
    END;

    INSERT INTO dbo.CompanyLocations
        (CompanyProfileId, [Name], [Address], Country, City, SortOrder)
    SELECT
        profile.Id,
        N'',
        profile.CompanyAddress,
        profile.CompanyCountry,
        profile.CompanyCity,
        0
    FROM dbo.CompanyProfiles AS profile
    WHERE
        (
            NULLIF(LTRIM(RTRIM(profile.CompanyAddress)), N'') IS NOT NULL
            OR NULLIF(LTRIM(RTRIM(profile.CompanyCountry)), N'') IS NOT NULL
            OR NULLIF(LTRIM(RTRIM(profile.CompanyCity)), N'') IS NOT NULL
        )
        AND NOT EXISTS
        (
            SELECT 1
            FROM dbo.CompanyLocations AS location
            WHERE location.CompanyProfileId = profile.Id
        );

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_CompanyLocations_CompanyProfileId'
          AND object_id = OBJECT_ID(N'dbo.CompanyLocations')
    )
    BEGIN
        CREATE INDEX IX_CompanyLocations_CompanyProfileId
            ON dbo.CompanyLocations(CompanyProfileId);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_Vacancies_CompanyLocationId'
          AND object_id = OBJECT_ID(N'dbo.Vacancies')
    )
    BEGIN
        CREATE INDEX IX_Vacancies_CompanyLocationId
            ON dbo.Vacancies(CompanyLocationId);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.foreign_keys
        WHERE [name] = N'FK_Vacancies_CompanyLocations_CompanyLocationId'
          AND parent_object_id = OBJECT_ID(N'dbo.Vacancies')
    )
    BEGIN
        ALTER TABLE dbo.Vacancies WITH CHECK
        ADD CONSTRAINT FK_Vacancies_CompanyLocations_CompanyLocationId
            FOREIGN KEY (CompanyLocationId)
            REFERENCES dbo.CompanyLocations(Id)
            ON DELETE SET NULL;

        ALTER TABLE dbo.Vacancies
            CHECK CONSTRAINT FK_Vacancies_CompanyLocations_CompanyLocationId;
    END;

    IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
       AND NOT EXISTS
       (
           SELECT 1
           FROM dbo.__EFMigrationsHistory
           WHERE MigrationId = N'20260819120000_AddCompanyLocationsAndLogo'
       )
    BEGIN
        INSERT INTO dbo.__EFMigrationsHistory(MigrationId, ProductVersion)
        VALUES
            (N'20260819120000_AddCompanyLocationsAndLogo', N'8.0.10');
    END;

    COMMIT TRANSACTION;

    SELECT
        (SELECT COUNT(*) FROM dbo.CompanyLocations) AS CompanyLocationCount,
        COL_LENGTH(N'dbo.CompanyProfiles', N'LogoDataUrl') AS LogoColumnBytes,
        COL_LENGTH(N'dbo.Vacancies', N'CompanyLocationId') AS VacancyLocationIdColumnBytes,
        COL_LENGTH(N'dbo.Vacancies', N'LocationName') AS VacancyLocationNameColumnBytes;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
