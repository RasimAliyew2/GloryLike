IF OBJECT_ID(N'dbo.UserSkills', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserSkills
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserSkills PRIMARY KEY,
        UserId INT NOT NULL,

        SkillId INT NOT NULL CONSTRAINT DF_UserSkills_SkillId DEFAULT 0,
        SkillName NVARCHAR(150) NOT NULL,

        PositionId INT NOT NULL CONSTRAINT DF_UserSkills_PositionId DEFAULT 0,
        PositionName NVARCHAR(150) NOT NULL CONSTRAINT DF_UserSkills_PositionName DEFAULT N'',

        SeniorityId INT NOT NULL CONSTRAINT DF_UserSkills_SeniorityId DEFAULT 0,
        SeniorityName NVARCHAR(50) NOT NULL CONSTRAINT DF_UserSkills_SeniorityName DEFAULT N'',

        JobFamilyId INT NOT NULL CONSTRAINT DF_UserSkills_JobFamilyId DEFAULT 0,
        JobFamilyName NVARCHAR(150) NOT NULL CONSTRAINT DF_UserSkills_JobFamilyName DEFAULT N'',

        SkillComplexity NVARCHAR(30) NOT NULL CONSTRAINT DF_UserSkills_SkillComplexity DEFAULT N'medium',
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_UserSkills_Status DEFAULT N'self_declared',
        IsVerified BIT NOT NULL CONSTRAINT DF_UserSkills_IsVerified DEFAULT 0,

        KnowledgeScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_KnowledgeScore DEFAULT 0,
        ExperienceScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_ExperienceScore DEFAULT 0,
        DepthScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_DepthScore DEFAULT 0,
        CredibilityScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_CredibilityScore DEFAULT 0,

        TaskComplexity NVARCHAR(50) NOT NULL CONSTRAINT DF_UserSkills_TaskComplexity DEFAULT N'',
        OwnershipLevel NVARCHAR(50) NOT NULL CONSTRAINT DF_UserSkills_OwnershipLevel DEFAULT N'',
        DepthTier NVARCHAR(50) NOT NULL CONSTRAINT DF_UserSkills_DepthTier DEFAULT N'',

        ContextScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_ContextScore DEFAULT 0,
        ComplexityScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_ComplexityScore DEFAULT 0,
        OwnershipScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_OwnershipScore DEFAULT 0,
        ResultScore FLOAT NOT NULL CONSTRAINT DF_UserSkills_ResultScore DEFAULT 0,

        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserSkills_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserSkills_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_UserSkills_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_UserSkills_UserId ON dbo.UserSkills(UserId);
    CREATE UNIQUE INDEX UX_UserSkills_UserId_SkillName ON dbo.UserSkills(UserId, SkillName);
END;
GO

IF OBJECT_ID(N'dbo.UserWorkExperiences', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserWorkExperiences
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserWorkExperiences PRIMARY KEY,
        UserId INT NOT NULL,

        CompanyName NVARCHAR(150) NOT NULL,
        PositionName NVARCHAR(150) NOT NULL CONSTRAINT DF_UserWorkExperiences_PositionName DEFAULT N'',
        StartYear NVARCHAR(30) NOT NULL CONSTRAINT DF_UserWorkExperiences_StartYear DEFAULT N'',
        EndYear NVARCHAR(30) NOT NULL CONSTRAINT DF_UserWorkExperiences_EndYear DEFAULT N'',
        FileName NVARCHAR(260) NOT NULL CONSTRAINT DF_UserWorkExperiences_FileName DEFAULT N'',

        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserWorkExperiences_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserWorkExperiences_UpdatedAt DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_UserWorkExperiences_Users
            FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_UserWorkExperiences_UserId ON dbo.UserWorkExperiences(UserId);
END;
GO
