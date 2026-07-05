IF OBJECT_ID('dbo.SkillQuestionnaires', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SkillQuestionnaires (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SkillQuestionnaires PRIMARY KEY DEFAULT NEWID(),
        SkillId INT NULL,
        SkillName NVARCHAR(150) NOT NULL,
        Seniority NVARCHAR(20) NOT NULL,
        SkillComplexity NVARCHAR(20) NOT NULL,
        Language NVARCHAR(10) NOT NULL DEFAULT 'az',
        QuestionCount INT NOT NULL,
        StructureJson NVARCHAR(MAX) NOT NULL,
        Version INT NOT NULL DEFAULT 1,
        GeneratedByModel NVARCHAR(50) NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'active',
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT CK_SkillQuestionnaires_StructureJson_IsJson
            CHECK (ISJSON(StructureJson) = 1)
    );

    CREATE INDEX IX_SkillQuestionnaires_CacheLookup
    ON dbo.SkillQuestionnaires (
        SkillName,
        Seniority,
        SkillComplexity,
        Language,
        Version,
        Status
    );
END
GO
