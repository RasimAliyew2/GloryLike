SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
        THROW 51220, 'dbo.Users was not found. Run this script against the BothFind application database.', 1;

    IF OBJECT_ID(N'dbo.CompanyStructureDepartments', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CompanyStructureDepartments
        (
            Id int IDENTITY(1, 1) NOT NULL
                CONSTRAINT PK_CompanyStructureDepartments PRIMARY KEY,
            CompanyOwnerUserId int NOT NULL,
            [Name] nvarchar(120) NOT NULL,
            SortOrder int NOT NULL,
            CreatedAtUtc datetime2 NOT NULL,
            UpdatedAtUtc datetime2 NOT NULL,
            CONSTRAINT FK_CompanyStructureDepartments_Users_CompanyOwnerUserId
                FOREIGN KEY (CompanyOwnerUserId)
                REFERENCES dbo.Users(Id)
        );
    END;

    IF OBJECT_ID(N'dbo.CompanyStructureDivisions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CompanyStructureDivisions
        (
            Id int IDENTITY(1, 1) NOT NULL
                CONSTRAINT PK_CompanyStructureDivisions PRIMARY KEY,
            DepartmentId int NOT NULL,
            [Name] nvarchar(120) NOT NULL,
            SortOrder int NOT NULL,
            CONSTRAINT FK_CompanyStructureDivisions_CompanyStructureDepartments_DepartmentId
                FOREIGN KEY (DepartmentId)
                REFERENCES dbo.CompanyStructureDepartments(Id)
                ON DELETE CASCADE
        );
    END;

    IF OBJECT_ID(N'dbo.CompanyStructurePositions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.CompanyStructurePositions
        (
            Id int IDENTITY(1, 1) NOT NULL
                CONSTRAINT PK_CompanyStructurePositions PRIMARY KEY,
            DivisionId int NOT NULL,
            [Name] nvarchar(160) NOT NULL,
            SortOrder int NOT NULL,
            CONSTRAINT FK_CompanyStructurePositions_CompanyStructureDivisions_DivisionId
                FOREIGN KEY (DivisionId)
                REFERENCES dbo.CompanyStructureDivisions(Id)
                ON DELETE CASCADE
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_CompanyStructureDepartments_Owner_SortOrder'
          AND object_id = OBJECT_ID(N'dbo.CompanyStructureDepartments')
    )
    BEGIN
        CREATE INDEX IX_CompanyStructureDepartments_Owner_SortOrder
            ON dbo.CompanyStructureDepartments(CompanyOwnerUserId, SortOrder);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'UX_CompanyStructureDepartments_Owner_Name'
          AND object_id = OBJECT_ID(N'dbo.CompanyStructureDepartments')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_CompanyStructureDepartments_Owner_Name
            ON dbo.CompanyStructureDepartments(CompanyOwnerUserId, [Name]);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_CompanyStructureDivisions_Department_SortOrder'
          AND object_id = OBJECT_ID(N'dbo.CompanyStructureDivisions')
    )
    BEGIN
        CREATE INDEX IX_CompanyStructureDivisions_Department_SortOrder
            ON dbo.CompanyStructureDivisions(DepartmentId, SortOrder);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'UX_CompanyStructureDivisions_Department_Name'
          AND object_id = OBJECT_ID(N'dbo.CompanyStructureDivisions')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_CompanyStructureDivisions_Department_Name
            ON dbo.CompanyStructureDivisions(DepartmentId, [Name]);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'IX_CompanyStructurePositions_Division_SortOrder'
          AND object_id = OBJECT_ID(N'dbo.CompanyStructurePositions')
    )
    BEGIN
        CREATE INDEX IX_CompanyStructurePositions_Division_SortOrder
            ON dbo.CompanyStructurePositions(DivisionId, SortOrder);
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE [name] = N'UX_CompanyStructurePositions_Division_Name'
          AND object_id = OBJECT_ID(N'dbo.CompanyStructurePositions')
    )
    BEGIN
        CREATE UNIQUE INDEX UX_CompanyStructurePositions_Division_Name
            ON dbo.CompanyStructurePositions(DivisionId, [Name]);
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    OBJECT_ID(N'dbo.CompanyStructureDepartments', N'U') AS DepartmentTableId,
    OBJECT_ID(N'dbo.CompanyStructureDivisions', N'U') AS DivisionTableId,
    OBJECT_ID(N'dbo.CompanyStructurePositions', N'U') AS PositionTableId;
