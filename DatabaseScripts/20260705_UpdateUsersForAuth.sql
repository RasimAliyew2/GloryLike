-- Run this on the backend MS SQL database if you do NOT use EF migrations.
-- It updates the existing Users table for registration/sign-in/forgot-password.

IF COL_LENGTH('Users', 'UserName') IS NULL
BEGIN
    ALTER TABLE Users ADD UserName NVARCHAR(80) NULL;
END

IF COL_LENGTH('Users', 'PhoneNumber') IS NULL
BEGIN
    ALTER TABLE Users ADD PhoneNumber NVARCHAR(30) NULL;
END

IF COL_LENGTH('Users', 'PasswordResetCodeHash') IS NULL
BEGIN
    ALTER TABLE Users ADD PasswordResetCodeHash NVARCHAR(500) NULL;
END

IF COL_LENGTH('Users', 'PasswordResetCodeExpiresAt') IS NULL
BEGIN
    ALTER TABLE Users ADD PasswordResetCodeExpiresAt DATETIME2 NULL;
END

IF COL_LENGTH('Users', 'CreatedAt') IS NULL
BEGIN
    ALTER TABLE Users ADD CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME();
END

IF COL_LENGTH('Users', 'UpdatedAt') IS NULL
BEGIN
    ALTER TABLE Users ADD UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT SYSUTCDATETIME();
END

-- Existing rows üçün boş unique field-ləri doldururuq ki, unique index yaradılanda problem çıxmasın.
UPDATE Users
SET UserName = CONCAT(N'user_', Id)
WHERE UserName IS NULL OR LTRIM(RTRIM(UserName)) = N'';

UPDATE Users
SET PhoneNumber = CONCAT(N'+994000000', Id)
WHERE PhoneNumber IS NULL OR LTRIM(RTRIM(PhoneNumber)) = N'';

UPDATE Users
SET Email = CONCAT(N'user_', Id, N'@example.com')
WHERE Email IS NULL OR LTRIM(RTRIM(Email)) = N'';

UPDATE Users
SET Name = N'User'
WHERE Name IS NULL OR LTRIM(RTRIM(Name)) = N'';

UPDATE Users
SET Surname = N'Unknown'
WHERE Surname IS NULL OR LTRIM(RTRIM(Surname)) = N'';

UPDATE Users
SET PasswordHash = N'RESET_REQUIRED'
WHERE PasswordHash IS NULL OR LTRIM(RTRIM(PasswordHash)) = N'';

ALTER TABLE Users ALTER COLUMN UserName NVARCHAR(80) NOT NULL;
ALTER TABLE Users ALTER COLUMN PhoneNumber NVARCHAR(30) NOT NULL;
ALTER TABLE Users ALTER COLUMN Email NVARCHAR(150) NOT NULL;
ALTER TABLE Users ALTER COLUMN Name NVARCHAR(80) NOT NULL;
ALTER TABLE Users ALTER COLUMN Surname NVARCHAR(80) NOT NULL;
ALTER TABLE Users ALTER COLUMN PasswordHash NVARCHAR(500) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_Email' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_Email ON Users(Email);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_PhoneNumber' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_PhoneNumber ON Users(PhoneNumber);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UX_Users_UserName' AND object_id = OBJECT_ID('Users'))
BEGIN
    CREATE UNIQUE INDEX UX_Users_UserName ON Users(UserName);
END
