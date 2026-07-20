SET XACT_ABORT ON;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Vacancies', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.Vacancies
        (
            Id INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_Vacancies PRIMARY KEY,
            EmployerUserId INT NOT NULL,
            PlatformVacancyId NVARCHAR(40) NOT NULL,
            JobFamilyId INT NOT NULL,
            SeniorityId INT NOT NULL,
            PositionId INT NOT NULL,
            JobFamilyName NVARCHAR(200) NOT NULL,
            SeniorityName NVARCHAR(100) NOT NULL,
            PositionName NVARCHAR(200) NOT NULL,
            RoleTitle NVARCHAR(200) NOT NULL,
            ClientRequisitionCode NVARCHAR(100) NOT NULL,
            EmploymentType NVARCHAR(50) NOT NULL,
            ExperienceRequired NVARCHAR(50) NOT NULL,
            EducationRequirement NVARCHAR(50) NOT NULL,
            EducationLevel NVARCHAR(100) NOT NULL,
            MinSalary DECIMAL(18,2) NULL,
            MaxSalary DECIMAL(18,2) NULL,
            PaymentTerms NVARCHAR(50) NOT NULL,
            Currency NVARCHAR(10) NOT NULL,
            HideSalary BIT NOT NULL,
            JobDescription NVARCHAR(5000) NOT NULL,
            MinimumVerificationLevel INT NOT NULL,
            MinimumMatchScore INT NOT NULL,
            MinimumTrustScore INT NOT NULL,
            AutoRejectBelowScore BIT NOT NULL,
            RequireVerifiedCoreSkills BIT NOT NULL,
            ScreeningNotes NVARCHAR(5000) NOT NULL,
            StageApplied BIT NOT NULL,
            StageScreening BIT NOT NULL,
            StageInterview BIT NOT NULL,
            StageOffer BIT NOT NULL,
            InterviewRounds INT NOT NULL,
            ScreeningSlaDays INT NOT NULL,
            Visibility NVARCHAR(20) NOT NULL,
            PublishDate DATETIME2 NULL,
            ApplicationDeadline DATETIME2 NULL,
            ContactEmail NVARCHAR(150) NOT NULL,
            AllowInternalCandidates BIT NOT NULL,
            NotifyMatchingCandidates BIT NOT NULL,
            PublicationPriority INT NOT NULL,
            [Status] NVARCHAR(20) NOT NULL,
            SourcePayloadJson NVARCHAR(MAX) NOT NULL,
            CreatedAtUtc DATETIME2 NOT NULL,
            UpdatedAtUtc DATETIME2 NOT NULL,

            CONSTRAINT FK_Vacancies_Users_EmployerUserId
                FOREIGN KEY (EmployerUserId)
                REFERENCES dbo.Users (Id),
            CONSTRAINT FK_Vacancies_JobFamilies_JobFamilyId
                FOREIGN KEY (JobFamilyId)
                REFERENCES dbo.JobFamilies (Id),
            CONSTRAINT FK_Vacancies_Seniorities_SeniorityId
                FOREIGN KEY (SeniorityId)
                REFERENCES dbo.Seniorities (Id),
            CONSTRAINT FK_Vacancies_Positions_PositionId
                FOREIGN KEY (PositionId)
                REFERENCES dbo.Positions (Id),
            CONSTRAINT CK_Vacancies_SalaryRange
                CHECK (
                    MinSalary IS NULL
                    OR MaxSalary IS NULL
                    OR MinSalary <= MaxSalary),
            CONSTRAINT CK_Vacancies_PublicationPriority
                CHECK (PublicationPriority BETWEEN 1 AND 10),
            CONSTRAINT CK_Vacancies_Visibility
                CHECK (Visibility IN (N'Public', N'Internal', N'Anonymous'))
        );
    END;

    IF OBJECT_ID(N'dbo.VacancySkillRequirements', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VacancySkillRequirements
        (
            Id INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_VacancySkillRequirements PRIMARY KEY,
            VacancyId INT NOT NULL,
            SkillId INT NOT NULL,
            SkillName NVARCHAR(200) NOT NULL,
            MinimumVerificationLevel INT NOT NULL,
            RequirementType NVARCHAR(20) NOT NULL,
            SortOrder INT NOT NULL,

            CONSTRAINT FK_VacancySkillRequirements_Vacancies_VacancyId
                FOREIGN KEY (VacancyId)
                REFERENCES dbo.Vacancies (Id)
                ON DELETE CASCADE,
            CONSTRAINT FK_VacancySkillRequirements_Skills_SkillId
                FOREIGN KEY (SkillId)
                REFERENCES dbo.Skills (Id),
            CONSTRAINT CK_VacancySkillRequirements_VerificationLevel
                CHECK (MinimumVerificationLevel BETWEEN 1 AND 100),
            CONSTRAINT CK_VacancySkillRequirements_RequirementType
                CHECK (RequirementType IN (N'Required', N'Desirable'))
        );
    END;

    IF OBJECT_ID(N'dbo.VacancyBenefits', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VacancyBenefits
        (
            Id INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_VacancyBenefits PRIMARY KEY,
            VacancyId INT NOT NULL,
            Name NVARCHAR(100) NOT NULL,
            SortOrder INT NOT NULL,

            CONSTRAINT FK_VacancyBenefits_Vacancies_VacancyId
                FOREIGN KEY (VacancyId)
                REFERENCES dbo.Vacancies (Id)
                ON DELETE CASCADE
        );
    END;

    IF OBJECT_ID(N'dbo.VacancyApplicationRequirements', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VacancyApplicationRequirements
        (
            Id INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_VacancyApplicationRequirements PRIMARY KEY,
            VacancyId INT NOT NULL,
            FieldKey NVARCHAR(100) NOT NULL,
            Label NVARCHAR(100) NOT NULL,
            RequirementMode NVARCHAR(20) NOT NULL,
            IsCustom BIT NOT NULL,
            SortOrder INT NOT NULL,

            CONSTRAINT FK_VacancyApplicationRequirements_Vacancies_VacancyId
                FOREIGN KEY (VacancyId)
                REFERENCES dbo.Vacancies (Id)
                ON DELETE CASCADE,
            CONSTRAINT CK_VacancyApplicationRequirements_Mode
                CHECK (RequirementMode IN (N'Required', N'Optional', N'Hidden'))
        );
    END;

    IF OBJECT_ID(N'dbo.VacancyScreeningQuestions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VacancyScreeningQuestions
        (
            Id INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_VacancyScreeningQuestions PRIMARY KEY,
            VacancyId INT NOT NULL,
            QuestionText NVARCHAR(500) NOT NULL,
            AnswerType NVARCHAR(20) NOT NULL,
            RequirementType NVARCHAR(20) NOT NULL,
            SortOrder INT NOT NULL,

            CONSTRAINT FK_VacancyScreeningQuestions_Vacancies_VacancyId
                FOREIGN KEY (VacancyId)
                REFERENCES dbo.Vacancies (Id)
                ON DELETE CASCADE,
            CONSTRAINT CK_VacancyScreeningQuestions_AnswerType
                CHECK (
                    AnswerType IN (
                        N'Text',
                        N'TrueFalse',
                        N'OneChoice',
                        N'ShortAnswer',
                        N'Number',
                        N'Date')),
            CONSTRAINT CK_VacancyScreeningQuestions_RequirementType
                CHECK (RequirementType IN (N'Required', N'KnockOut'))
        );
    END;

    IF OBJECT_ID(N'dbo.VacancyFunnelStages', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VacancyFunnelStages
        (
            Id INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_VacancyFunnelStages PRIMARY KEY,
            VacancyId INT NOT NULL,
            StageName NVARCHAR(100) NOT NULL,
            Hours INT NOT NULL,
            IsStandard BIT NOT NULL,
            SortOrder INT NOT NULL,

            CONSTRAINT FK_VacancyFunnelStages_Vacancies_VacancyId
                FOREIGN KEY (VacancyId)
                REFERENCES dbo.Vacancies (Id)
                ON DELETE CASCADE,
            CONSTRAINT CK_VacancyFunnelStages_Hours
                CHECK (Hours BETWEEN 0 AND 8760)
        );
    END;

    IF OBJECT_ID(N'dbo.VacancyPublicationChannels', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.VacancyPublicationChannels
        (
            Id INT IDENTITY(1,1) NOT NULL
                CONSTRAINT PK_VacancyPublicationChannels PRIMARY KEY,
            VacancyId INT NOT NULL,
            ChannelType NVARCHAR(20) NOT NULL,
            ChannelName NVARCHAR(50) NOT NULL,
            IsEnabled BIT NOT NULL,
            SortOrder INT NOT NULL,

            CONSTRAINT FK_VacancyPublicationChannels_Vacancies_VacancyId
                FOREIGN KEY (VacancyId)
                REFERENCES dbo.Vacancies (Id)
                ON DELETE CASCADE,
            CONSTRAINT CK_VacancyPublicationChannels_ChannelType
                CHECK (ChannelType IN (N'Core', N'Outdoor', N'Social'))
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_Vacancies_PlatformVacancyId'
          AND object_id = OBJECT_ID(N'dbo.Vacancies')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_Vacancies_PlatformVacancyId
            ON dbo.Vacancies (PlatformVacancyId);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Vacancies_EmployerUserId'
          AND object_id = OBJECT_ID(N'dbo.Vacancies')
    )
    BEGIN
        CREATE INDEX IX_Vacancies_EmployerUserId
            ON dbo.Vacancies (EmployerUserId);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Vacancies_PositionId'
          AND object_id = OBJECT_ID(N'dbo.Vacancies')
    )
    BEGIN
        CREATE INDEX IX_Vacancies_PositionId
            ON dbo.Vacancies (PositionId);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_Vacancies_CreatedAtUtc'
          AND object_id = OBJECT_ID(N'dbo.Vacancies')
    )
    BEGIN
        CREATE INDEX IX_Vacancies_CreatedAtUtc
            ON dbo.Vacancies (CreatedAtUtc DESC);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_VacancySkillRequirements_VacancyId_SkillId'
          AND object_id = OBJECT_ID(N'dbo.VacancySkillRequirements')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_VacancySkillRequirements_VacancyId_SkillId
            ON dbo.VacancySkillRequirements (VacancyId, SkillId);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_VacancyBenefits_VacancyId_Name'
          AND object_id = OBJECT_ID(N'dbo.VacancyBenefits')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_VacancyBenefits_VacancyId_Name
            ON dbo.VacancyBenefits (VacancyId, Name);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_VacancyApplicationRequirements_VacancyId_FieldKey'
          AND object_id = OBJECT_ID(N'dbo.VacancyApplicationRequirements')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_VacancyApplicationRequirements_VacancyId_FieldKey
            ON dbo.VacancyApplicationRequirements (VacancyId, FieldKey);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_VacancyScreeningQuestions_VacancyId'
          AND object_id = OBJECT_ID(N'dbo.VacancyScreeningQuestions')
    )
    BEGIN
        CREATE INDEX IX_VacancyScreeningQuestions_VacancyId
            ON dbo.VacancyScreeningQuestions (VacancyId, SortOrder);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'IX_VacancyFunnelStages_VacancyId'
          AND object_id = OBJECT_ID(N'dbo.VacancyFunnelStages')
    )
    BEGIN
        CREATE INDEX IX_VacancyFunnelStages_VacancyId
            ON dbo.VacancyFunnelStages (VacancyId, SortOrder);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE name = N'UX_VacancyPublicationChannels_VacancyId_ChannelName'
          AND object_id = OBJECT_ID(N'dbo.VacancyPublicationChannels')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_VacancyPublicationChannels_VacancyId_ChannelName
            ON dbo.VacancyPublicationChannels (VacancyId, ChannelName);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO
