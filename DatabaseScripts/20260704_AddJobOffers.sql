IF OBJECT_ID(N'dbo.JobOffers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.JobOffers
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_JobOffers PRIMARY KEY,
        RequiredJob NVARCHAR(150) NOT NULL,
        Seniority NVARCHAR(50) NOT NULL,
        Skills NVARCHAR(MAX) NOT NULL,
        SkillsWeight INT NOT NULL
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_JobOffers_RequiredJob_Seniority'
      AND object_id = OBJECT_ID(N'dbo.JobOffers')
)
BEGIN
    CREATE INDEX IX_JobOffers_RequiredJob_Seniority
    ON dbo.JobOffers (RequiredJob, Seniority);
END;
GO
