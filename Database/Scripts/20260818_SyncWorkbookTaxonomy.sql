-- Corrected taxonomy import generated from every Job Family sheet and All Positions.
-- Workbook facts: 18 Job Families, 260 unique positions,
-- 113 core skill definitions, 447 position skill definitions,
-- 229 All Positions roles x 5 generated role-specific skills,
-- 3208 final active Position + Skill assignments.
--
-- Rules implemented:
--  * "Core Skills" is a section label and is never inserted as a selectable skill.
--  * Actual skills below Core Skills are copied to every position in that Job Family.
--  * Position section skills are attached only to that section's position.
--  * Skill eligibility starts at MinimumSenioritySortOrder and continues upward.
--  * All Positions roles become selectable at the row seniority or any higher level.
--  * Duplicate Job Family + Position names are upserted once and keep the lowest row threshold.
--  * Legacy rows are preserved for FK safety, but stale skills are marked inactive and stale
--    position/seniority links in the imported Job Families are removed.

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.JobFamilies', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Positions', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Seniorities', N'U') IS NULL
   OR OBJECT_ID(N'dbo.PositionSeniorities', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Skills', N'U') IS NULL
BEGIN
    THROW 51030, 'Run the backend database migrations before the workbook taxonomy import.', 1;
END;

IF COL_LENGTH(N'dbo.Skills', N'MinimumSenioritySortOrder') IS NULL
    ALTER TABLE dbo.Skills ADD MinimumSenioritySortOrder INT NOT NULL
        CONSTRAINT DF_Skills_MinimumSenioritySortOrder DEFAULT (1);

IF COL_LENGTH(N'dbo.Skills', N'IsCore') IS NULL
    ALTER TABLE dbo.Skills ADD IsCore BIT NOT NULL
        CONSTRAINT DF_Skills_IsCore DEFAULT (0);

IF COL_LENGTH(N'dbo.Skills', N'IsActive') IS NULL
    ALTER TABLE dbo.Skills ADD IsActive BIT NOT NULL
        CONSTRAINT DF_Skills_IsActive DEFAULT (1);

IF COL_LENGTH(N'dbo.Skills', N'AssessmentType') IS NULL
    ALTER TABLE dbo.Skills ADD AssessmentType NVARCHAR(10) NOT NULL
        CONSTRAINT DF_Skills_AssessmentType DEFAULT (N'TP');

IF COL_LENGTH(N'dbo.Skills', N'VerificationMethod') IS NULL
    ALTER TABLE dbo.Skills ADD VerificationMethod NVARCHAR(120) NOT NULL
        CONSTRAINT DF_Skills_VerificationMethod DEFAULT (N'');

-- SQL Server compiles a batch before running ALTER TABLE statements. Start the
-- data sync in a new batch so the new metadata columns are visible to the parser.
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    CREATE TABLE #SourcePositions
    (
        JobName NVARCHAR(150) NOT NULL,
        PositionName NVARCHAR(150) NOT NULL,
        MinimumSortOrder INT NOT NULL,
        IsInAllPositions BIT NOT NULL,
        CONSTRAINT PK_SourcePositions PRIMARY KEY (JobName, PositionName)
    );

    INSERT INTO #SourcePositions
    (
        JobName,
        PositionName,
        MinimumSortOrder,
        IsInAllPositions
    )
    VALUES
    (N'Administration & Office Management', N'Administrative Assistant', 1, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Administrative Manager', 3, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Corporate Secretary', 2, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Document Controller', 2, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Executive Assistant', 2, CAST(0 AS BIT)),
    (N'Administration & Office Management', N'Executive Assistant / PA', 2, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Head of Administration', 5, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Office Manager', 2, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Receptionist', 1, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Senior EA / Chief of Staff', 3, CAST(1 AS BIT)),
    (N'Administration & Office Management', N'Tender Specialist', 2, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'AML / Compliance', 2, CAST(0 AS BIT)),
    (N'Banking & Financial Services', N'AML Specialist', 2, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Banking Specialist', 1, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Branch Manager', 4, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Chief Risk Officer', 5, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Collection', 2, CAST(0 AS BIT)),
    (N'Banking & Financial Services', N'Collection Specialist', 2, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Compliance Officer', 3, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Credit Analyst', 2, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', 2, CAST(0 AS BIT)),
    (N'Banking & Financial Services', N'Credit Analyst Junior', 1, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Head of Compliance', 5, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Head of Retail Banking', 5, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Insurance', 2, CAST(0 AS BIT)),
    (N'Banking & Financial Services', N'Insurance Agent', 1, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Insurance Underwriter', 2, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Investment Analyst', 3, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Loan Officer', 2, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Microfinance Specialist', 2, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Risk & Compliance Lead', 4, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Risk Manager', 3, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Senior Credit Analyst', 3, CAST(1 AS BIT)),
    (N'Banking & Financial Services', N'Treasury', 2, CAST(0 AS BIT)),
    (N'Banking & Financial Services', N'Treasury Specialist', 3, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Architect', 2, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Chief Architect', 5, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Civil / Structural Engineer', 2, CAST(0 AS BIT)),
    (N'Construction & Civil Engineering', N'Civil Engineer', 2, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Construction Director', 5, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Design Manager', 4, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Head of Construction', 5, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'HSE Construction', 2, CAST(0 AS BIT)),
    (N'Construction & Civil Engineering', N'HSE Officer Construction', 3, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Junior Architect', 1, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'MEP Engineer', 2, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Project Manager', 2, CAST(0 AS BIT)),
    (N'Construction & Civil Engineering', N'Project Manager Construction', 4, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Quantity Surveyor', 2, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Quantity Surveyor Junior', 1, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Senior Architect', 3, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Senior Civil Engineer', 3, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Senior Structural Engineer', 3, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Site Engineer', 1, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Site Supervisor', 2, CAST(1 AS BIT)),
    (N'Construction & Civil Engineering', N'Structural Engineer', 2, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'3D Designer / Visualizer', 2, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Art Director', 4, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Creative Director', 5, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Creative Lead', 3, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Graphic Designer', 2, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Head of Creative', 5, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Junior Graphic Designer', 1, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Motion Designer', 2, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Motion Designer / Video Editor', 2, CAST(0 AS BIT)),
    (N'Creative & Graphic Design', N'Photographer', 2, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Senior Graphic Designer', 3, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Video Editor', 2, CAST(1 AS BIT)),
    (N'Creative & Graphic Design', N'Video Editor Junior', 1, CAST(1 AS BIT)),
    (N'Education & Training', N'Academic Coordinator', 3, CAST(1 AS BIT)),
    (N'Education & Training', N'Academic Director', 5, CAST(1 AS BIT)),
    (N'Education & Training', N'Corporate Trainer', 2, CAST(1 AS BIT)),
    (N'Education & Training', N'Curriculum Developer', 2, CAST(1 AS BIT)),
    (N'Education & Training', N'English Teacher', 2, CAST(0 AS BIT)),
    (N'Education & Training', N'Head of Corporate Learning', 4, CAST(1 AS BIT)),
    (N'Education & Training', N'Head of Training Center', 4, CAST(1 AS BIT)),
    (N'Education & Training', N'Senior Teacher / Methodologist', 3, CAST(1 AS BIT)),
    (N'Education & Training', N'Teacher English / Math / Sciences', 2, CAST(1 AS BIT)),
    (N'Education & Training', N'Teaching Assistant', 1, CAST(1 AS BIT)),
    (N'Education & Training', N'Training Center Administrator', 2, CAST(1 AS BIT)),
    (N'Education & Training', N'Tutor', 1, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Backend Developer', 1, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'CTO', 5, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Data Engineer', 2, CAST(0 AS BIT)),
    (N'Engineering (IT)', N'DevOps Engineer', 2, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Engineering Lead', 4, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Frontend Developer', 1, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Head of Engineering', 5, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Mobile Developer', 1, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'QA Engineer', 1, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Senior / Lead Additions', 3, CAST(0 AS BIT)),
    (N'Engineering (IT)', N'Senior Backend Developer', 3, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Senior DevOps Engineer', 3, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Senior Frontend Developer', 3, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Senior QA Engineer', 3, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'Tech Lead', 4, CAST(1 AS BIT)),
    (N'Engineering (IT)', N'VP of Engineering', 5, CAST(1 AS BIT)),
    (N'Finance', N'Accountant', 2, CAST(1 AS BIT)),
    (N'Finance', N'CFO', 5, CAST(1 AS BIT)),
    (N'Finance', N'Finance Director', 5, CAST(1 AS BIT)),
    (N'Finance', N'Finance Manager', 4, CAST(1 AS BIT)),
    (N'Finance', N'Financial Analyst', 2, CAST(1 AS BIT)),
    (N'Finance', N'Financial Analyst / FP&A', 2, CAST(0 AS BIT)),
    (N'Finance', N'FP&A Manager', 4, CAST(1 AS BIT)),
    (N'Finance', N'Head of Finance', 5, CAST(1 AS BIT)),
    (N'Finance', N'Junior Accountant', 1, CAST(1 AS BIT)),
    (N'Finance', N'Junior Financial Analyst', 1, CAST(1 AS BIT)),
    (N'Finance', N'Senior / Lead', 3, CAST(0 AS BIT)),
    (N'Finance', N'Senior Accountant', 3, CAST(1 AS BIT)),
    (N'Finance', N'Senior Financial Analyst', 3, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Event Coordinator', 2, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Event Manager', 2, CAST(0 AS BIT)),
    (N'Hospitality & Tourism', N'F&B Manager', 2, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Front Office / Reservations', 2, CAST(0 AS BIT)),
    (N'Hospitality & Tourism', N'Front Office Manager', 2, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'General Manager Hotel', 4, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Hotel Operations Manager', 3, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Receptionist Hotel', 1, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Restaurant Manager', 2, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Revenue Manager', 2, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Senior Event Manager', 3, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Tour Guide', 1, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'VP of Hospitality', 5, CAST(1 AS BIT)),
    (N'Hospitality & Tourism', N'Waiter / Bartender', 1, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Chief Compliance Officer', 4, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Compliance Assistant', 1, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Compliance Manager', 3, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Compliance Specialist', 2, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Contract Specialist', 2, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Corporate Lawyer', 2, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'General Counsel', 5, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Head of Legal', 4, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Labor / Employment Law', 2, CAST(0 AS BIT)),
    (N'Legal & Compliance', N'Legal Assistant', 1, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Legal Counsel', 2, CAST(1 AS BIT)),
    (N'Legal & Compliance', N'Senior Legal Counsel', 3, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Chief Supply Chain Officer', 5, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Customs Specialist', 2, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Fleet Manager', 4, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Freight Forwarder', 2, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Head of Logistics', 5, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Import / Export Manager', 3, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Logistics Coordinator', 1, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Logistics Specialist', 2, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Procurement', 2, CAST(0 AS BIT)),
    (N'Logistics & Supply Chain', N'Procurement Manager', 3, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Procurement Specialist', 2, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Senior Logistics Manager', 3, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Supply Chain Manager', 4, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Supply Chain Specialist', 2, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Warehouse Assistant', 1, CAST(1 AS BIT)),
    (N'Logistics & Supply Chain', N'Warehouse Manager', 2, CAST(1 AS BIT)),
    (N'Marketing', N'Brand / Content', 2, CAST(0 AS BIT)),
    (N'Marketing', N'CMO', 5, CAST(1 AS BIT)),
    (N'Marketing', N'Content Manager', 2, CAST(1 AS BIT)),
    (N'Marketing', N'Content Writer', 1, CAST(1 AS BIT)),
    (N'Marketing', N'Digital Marketing', 2, CAST(0 AS BIT)),
    (N'Marketing', N'Digital Marketing Manager', 2, CAST(1 AS BIT)),
    (N'Marketing', N'Head of Marketing', 5, CAST(1 AS BIT)),
    (N'Marketing', N'Marketing Specialist', 1, CAST(1 AS BIT)),
    (N'Marketing', N'Marketing Team Lead', 4, CAST(1 AS BIT)),
    (N'Marketing', N'Senior Digital Marketing Manager', 3, CAST(1 AS BIT)),
    (N'Marketing', N'Senior SEO Specialist', 3, CAST(1 AS BIT)),
    (N'Marketing', N'SEO Specialist', 2, CAST(1 AS BIT)),
    (N'Marketing', N'SMM', 2, CAST(0 AS BIT)),
    (N'Marketing', N'SMM Manager', 2, CAST(1 AS BIT)),
    (N'Marketing', N'SMM Specialist', 1, CAST(1 AS BIT)),
    (N'Marketing', N'VP of Marketing', 5, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Drilling Engineer', 2, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Field Operator', 1, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Head of HSE', 5, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'HSE', 2, CAST(0 AS BIT)),
    (N'Oil, Gas & Energy', N'HSE Coordinator', 1, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'HSE Manager', 3, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'HSE Specialist', 2, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Instrument Engineer', 2, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Junior Process Engineer', 1, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Lead Engineer', 4, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Mechanical / Instrument', 2, CAST(0 AS BIT)),
    (N'Oil, Gas & Energy', N'Mechanical Engineer (O&G)', 2, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Operations Manager (O&G)', 5, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Process Engineer', 2, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Reservoir Engineer', 3, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Senior Drilling Engineer', 3, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Senior Process Engineer', 3, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'Subsurface Lead', 4, CAST(1 AS BIT)),
    (N'Oil, Gas & Energy', N'VP of Engineering (O&G)', 5, CAST(1 AS BIT)),
    (N'Operations', N'Administration', 2, CAST(0 AS BIT)),
    (N'Operations', N'COO', 5, CAST(1 AS BIT)),
    (N'Operations', N'Head of Operations', 5, CAST(1 AS BIT)),
    (N'Operations', N'Junior Project Manager', 1, CAST(1 AS BIT)),
    (N'Operations', N'Operations Coordinator', 1, CAST(1 AS BIT)),
    (N'Operations', N'Operations Lead', 4, CAST(1 AS BIT)),
    (N'Operations', N'Operations Manager', 2, CAST(1 AS BIT)),
    (N'Operations', N'Portfolio Manager', 4, CAST(1 AS BIT)),
    (N'Operations', N'Program Manager', 3, CAST(1 AS BIT)),
    (N'Operations', N'Project Manager', 2, CAST(1 AS BIT)),
    (N'Operations', N'Senior Project Manager', 3, CAST(1 AS BIT)),
    (N'Operations', N'VP of Operations', 5, CAST(1 AS BIT)),
    (N'People & HR', N'CHRO', 5, CAST(1 AS BIT)),
    (N'People & HR', N'Head of HR', 5, CAST(1 AS BIT)),
    (N'People & HR', N'Head of Recruitment', 5, CAST(1 AS BIT)),
    (N'People & HR', N'HR Business Partner', 3, CAST(1 AS BIT)),
    (N'People & HR', N'HR Coordinator', 1, CAST(1 AS BIT)),
    (N'People & HR', N'HR Director', 5, CAST(1 AS BIT)),
    (N'People & HR', N'HR Lead', 4, CAST(1 AS BIT)),
    (N'People & HR', N'HR Manager', 2, CAST(1 AS BIT)),
    (N'People & HR', N'Junior Recruiter', 1, CAST(1 AS BIT)),
    (N'People & HR', N'L&D', 2, CAST(0 AS BIT)),
    (N'People & HR', N'L&D Specialist', 2, CAST(1 AS BIT)),
    (N'People & HR', N'Recruiter', 2, CAST(1 AS BIT)),
    (N'People & HR', N'Recruitment Team Lead', 4, CAST(1 AS BIT)),
    (N'People & HR', N'Senior Recruiter', 3, CAST(1 AS BIT)),
    (N'Product', N'Associate Product Manager', 1, CAST(1 AS BIT)),
    (N'Product', N'CPO', 5, CAST(1 AS BIT)),
    (N'Product', N'Group Product Manager', 4, CAST(1 AS BIT)),
    (N'Product', N'Head of Product', 5, CAST(1 AS BIT)),
    (N'Product', N'Principal Product Manager', 4, CAST(1 AS BIT)),
    (N'Product', N'Product Manager', 2, CAST(1 AS BIT)),
    (N'Product', N'Senior / Lead', 3, CAST(0 AS BIT)),
    (N'Product', N'Senior Product Manager', 3, CAST(1 AS BIT)),
    (N'Product', N'VP of Product', 5, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Call Center Operator', 1, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Call Center Supervisor', 3, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Call Center Team Lead', 2, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Cashier / Store Assistant', 1, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Customer Experience Manager', 3, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Customer Service Specialist', 2, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'CX Lead', 4, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Head of Customer Experience', 5, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Head of Retail', 5, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Regional Retail Manager', 4, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Retail Manager', 2, CAST(0 AS BIT)),
    (N'Retail & Customer Service', N'Retail Store Manager', 3, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Retail Store Supervisor', 2, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Sales Consultant', 1, CAST(1 AS BIT)),
    (N'Retail & Customer Service', N'Sales Consultant / Advisor', 2, CAST(0 AS BIT)),
    (N'Retail & Customer Service', N'Visual Merchandiser', 2, CAST(1 AS BIT)),
    (N'Sales', N'Account Executive', 2, CAST(1 AS BIT)),
    (N'Sales', N'Account Executive Junior', 1, CAST(1 AS BIT)),
    (N'Sales', N'BDM', 2, CAST(0 AS BIT)),
    (N'Sales', N'CSO', 5, CAST(1 AS BIT)),
    (N'Sales', N'Head of Sales', 5, CAST(1 AS BIT)),
    (N'Sales', N'Key Account Manager', 3, CAST(1 AS BIT)),
    (N'Sales', N'Sales Manager', 2, CAST(1 AS BIT)),
    (N'Sales', N'Sales Representative', 1, CAST(1 AS BIT)),
    (N'Sales', N'Sales Team Lead', 4, CAST(1 AS BIT)),
    (N'Sales', N'Senior / Lead', 3, CAST(0 AS BIT)),
    (N'Sales', N'Senior Sales Manager', 3, CAST(1 AS BIT)),
    (N'Sales', N'VP of Sales', 5, CAST(1 AS BIT)),
    (N'UX & Design', N'Head of Design', 5, CAST(1 AS BIT)),
    (N'UX & Design', N'Junior UI Designer', 1, CAST(1 AS BIT)),
    (N'UX & Design', N'Junior UX Designer', 1, CAST(1 AS BIT)),
    (N'UX & Design', N'Lead Designer', 4, CAST(1 AS BIT)),
    (N'UX & Design', N'Product Designer', 2, CAST(1 AS BIT)),
    (N'UX & Design', N'Senior Product Designer', 3, CAST(1 AS BIT)),
    (N'UX & Design', N'Senior UX Designer', 3, CAST(1 AS BIT)),
    (N'UX & Design', N'UI Designer', 2, CAST(1 AS BIT)),
    (N'UX & Design', N'UX Designer', 2, CAST(1 AS BIT)),
    (N'UX & Design', N'VP of Design', 5, CAST(1 AS BIT));

    CREATE TABLE #CoreSkillDefinitions
    (
        JobName NVARCHAR(150) NOT NULL,
        SkillName NVARCHAR(150) NOT NULL,
        MinimumSortOrder INT NOT NULL,
        AssessmentType NVARCHAR(10) NOT NULL,
        VerificationMethod NVARCHAR(120) NOT NULL
    );

    INSERT INTO #CoreSkillDefinitions
    (
        JobName,
        SkillName,
        MinimumSortOrder,
        AssessmentType,
        VerificationMethod
    )
    VALUES
    (N'Administration & Office Management', N'Azerbaijani / Russian / English', 1, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Business Communication', 1, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Confidentiality', 1, N'T', N'Knowledge test / Certificate'),
    (N'Administration & Office Management', N'Document Management', 1, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'MS Office Suite', 1, N'TP', N'Knowledge test + Experience'),
    (N'Administration & Office Management', N'Scheduling & Calendar Management', 1, N'P', N'Work history / Portfolio'),
    (N'Banking & Financial Services', N'Attention to Detail', 1, N'P', N'Work history / Portfolio'),
    (N'Banking & Financial Services', N'Banking Regulations AZ', 1, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Credit Scoring', 1, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Customer Communication', 1, N'P', N'Work history / Portfolio'),
    (N'Banking & Financial Services', N'Excel Advanced', 1, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Financial Analysis', 1, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Risk Assessment', 1, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'AutoCAD', 1, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Building Regulations AZ', 1, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'Construction Documentation', 1, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'MS Project / Primavera', 1, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Site Safety Awareness', 1, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'Technical Drawing Reading', 1, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Adobe Illustrator', 1, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Adobe Photoshop', 1, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Attention to Detail', 1, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Brand Guidelines Adherence', 1, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Color Theory', 1, N'T', N'Knowledge test / Certificate'),
    (N'Creative & Graphic Design', N'Typography', 1, N'T', N'Knowledge test / Certificate'),
    (N'Education & Training', N'Classroom Management', 1, N'P', N'Work history / Portfolio'),
    (N'Education & Training', N'Communication & Presentation', 1, N'P', N'Work history / Portfolio'),
    (N'Education & Training', N'Lesson Planning', 1, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'Student Assessment', 1, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'Subject Matter Expertise', 1, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Agile / Scrum', 1, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Code Review', 1, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Git & Version Control', 1, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Problem Solving', 1, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'Technical Documentation', 1, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Unit Testing', 1, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Analytical Thinking', 1, N'T', N'Knowledge test / Certificate'),
    (N'Finance', N'Attention to Detail', 1, N'P', N'Work history / Portfolio'),
    (N'Finance', N'Budget Planning', 1, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Excel Advanced', 1, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analysis', 1, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Reporting', 1, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'Complaint Handling', 1, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Guest Communication', 1, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Hospitality Service Standards', 1, N'T', N'Knowledge test / Certificate'),
    (N'Hospitality & Tourism', N'Multi-language Skills (AZ/RU/EN)', 1, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Opera PMS / Hotel Software', 1, N'P', N'Work history / Portfolio'),
    (N'Legal & Compliance', N'Azerbaijan Civil Law', 1, N'R', N'Recognized certification only'),
    (N'Legal & Compliance', N'Confidentiality Ethics', 1, N'T', N'Knowledge test / Certificate'),
    (N'Legal & Compliance', N'Contract Drafting', 1, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Legal Research', 1, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Legal Writing', 1, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'MS Office / Legal Software', 1, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Attention to Detail', 1, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Documentation & Reporting', 1, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Excel / ERP', 1, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Problem Solving Under Pressure', 1, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Supply Chain Fundamentals', 1, N'T', N'Knowledge test / Certificate'),
    (N'Logistics & Supply Chain', N'Vendor Communication', 1, N'P', N'Work history / Portfolio'),
    (N'Marketing', N'A/B Testing', 1, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Campaign Management', 1, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Content Strategy', 1, N'T', N'Knowledge test / Certificate'),
    (N'Marketing', N'Copywriting', 1, N'P', N'Work history / Portfolio'),
    (N'Marketing', N'Google Analytics / GA4', 1, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Marketing Strategy', 1, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Engineering Software (AutoCAD/PDMS)', 1, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'HSE Awareness', 1, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Oil & Gas Industry Knowledge', 1, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Project Management basics', 1, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Risk Assessment', 1, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Technical Report Writing', 1, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Problem Solving', 1, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Process Improvement', 1, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Project Management', 1, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Reporting & Documentation', 1, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Risk Management', 1, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Stakeholder Communication', 1, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'ATS Usage', 1, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'Candidate Assessment', 1, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'Communication', 1, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'Confidentiality & Ethics', 1, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'Interviewing Techniques', 1, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'Labor Law', 1, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Agile / Scrum', 1, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Backlog Management', 1, N'P', N'Work history / Portfolio'),
    (N'Product', N'Data-Driven Decision Making', 1, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'JIRA / Linear', 1, N'P', N'Work history / Portfolio'),
    (N'Product', N'Market Research', 1, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Prioritization Frameworks', 1, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Product Thinking', 1, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Roadmap Planning', 1, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Stakeholder Management', 1, N'P', N'Work history / Portfolio'),
    (N'Product', N'User Story Writing', 1, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Active Listening', 1, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Conflict Resolution', 1, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'CRM Basics', 1, N'T', N'Knowledge test / Certificate'),
    (N'Retail & Customer Service', N'Customer Communication', 1, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Product Knowledge', 1, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Teamwork', 1, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Active Listening', 1, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Communication', 1, N'P', N'Work history / Portfolio'),
    (N'Sales', N'CRM Salesforce/HubSpot/Bitrix', 1, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Negotiation', 1, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Objection Handling', 1, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Pipeline Management', 1, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Target Achievement', 1, N'P', N'Work history / Portfolio'),
    (N'UX & Design', N'Design Systems', 1, N'TP', N'Knowledge test + Experience'),
    (N'UX & Design', N'Figma', 1, N'TP', N'Knowledge test + Experience'),
    (N'UX & Design', N'Prototyping', 1, N'P', N'Work history / Portfolio'),
    (N'UX & Design', N'Responsive Design', 1, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'Typography & Color Theory', 1, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'Visual Design Principles', 1, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'Wireframing', 1, N'P', N'Work history / Portfolio');

    CREATE TABLE #PositionSkillDefinitions
    (
        JobName NVARCHAR(150) NOT NULL,
        PositionName NVARCHAR(150) NOT NULL,
        SkillName NVARCHAR(150) NOT NULL,
        MinimumSortOrder INT NOT NULL,
        AssessmentType NVARCHAR(10) NOT NULL,
        VerificationMethod NVARCHAR(120) NOT NULL
    );

    INSERT INTO #PositionSkillDefinitions
    (
        JobName,
        PositionName,
        SkillName,
        MinimumSortOrder,
        AssessmentType,
        VerificationMethod
    )
    VALUES
    (N'Administration & Office Management', N'Document Controller', N'Aconex / SharePoint', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Document Controller', N'Document Management Systems', 2, N'TP', N'Knowledge test + Experience'),
    (N'Administration & Office Management', N'Document Controller', N'Filing & Archiving', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Document Controller', N'ISO Documentation Standards', 2, N'T', N'Knowledge test / Certificate'),
    (N'Administration & Office Management', N'Document Controller', N'Version Control', 2, N'TP', N'Knowledge test + Experience'),
    (N'Administration & Office Management', N'Executive Assistant', N'C-level Support', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Executive Assistant', N'Discretion & Professionalism', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Executive Assistant', N'Expense Reporting', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Executive Assistant', N'Meeting Facilitation', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Executive Assistant', N'Travel Coordination', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Tender Specialist', N'Bid Evaluation', 2, N'T', N'Knowledge test / Certificate'),
    (N'Administration & Office Management', N'Tender Specialist', N'e-Procurement Platforms', 2, N'P', N'Work history / Portfolio'),
    (N'Administration & Office Management', N'Tender Specialist', N'Public Procurement Law AZ', 2, N'R', N'Recognized certification only'),
    (N'Administration & Office Management', N'Tender Specialist', N'Technical Specification Writing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Administration & Office Management', N'Tender Specialist', N'Tender Preparation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'AML / Compliance', N'AML / KYC Procedures', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'AML / Compliance', N'Basel III Knowledge', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'AML / Compliance', N'CAMS Certification', 2, N'R', N'Recognized certification only'),
    (N'Banking & Financial Services', N'AML / Compliance', N'Compliance Monitoring', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'AML / Compliance', N'FATF Standards', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'AML / Compliance', N'Internal Audit', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'AML / Compliance', N'Regulatory Reporting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Collection', N'Debt Collection Techniques', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Collection', N'Excel / 1C', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Collection', N'Legal Enforcement Basics', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Collection', N'Negotiation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Collection', N'Portfolio Monitoring', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', N'1C / Banking Software', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', N'Collateral Evaluation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', N'Credit Risk Assessment', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', N'Debt Restructuring', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', N'Financial Statement Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', N'Loan Portfolio Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Credit Analyst / Loan Officer', N'Microfinance Operations', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Insurance', N'Actuarial Basics', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Insurance', N'Claims Assessment', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Insurance', N'Insurance Regulations AZ', 2, N'R', N'Recognized certification only'),
    (N'Banking & Financial Services', N'Insurance', N'Product Knowledge (Life/Non-life)', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Insurance', N'Underwriting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Investment Analyst', N'Bloomberg Terminal', 2, N'P', N'Work history / Portfolio'),
    (N'Banking & Financial Services', N'Investment Analyst', N'Capital Markets', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Investment Analyst', N'CFA Level I+', 2, N'R', N'Recognized certification only'),
    (N'Banking & Financial Services', N'Investment Analyst', N'DCF Modeling', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Investment Analyst', N'Equity Research', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Investment Analyst', N'Portfolio Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Risk Manager', N'Credit Risk', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Risk Manager', N'Market Risk', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Risk Manager', N'Operational Risk', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Risk Manager', N'Risk Reporting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Risk Manager', N'Stress Testing', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Risk Manager', N'VaR Modeling', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Treasury', N'Cash Flow Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Treasury', N'FX Operations', 2, N'TP', N'Knowledge test + Experience'),
    (N'Banking & Financial Services', N'Treasury', N'Liquidity Management', 2, N'T', N'Knowledge test / Certificate'),
    (N'Banking & Financial Services', N'Treasury', N'SWIFT', 2, N'P', N'Work history / Portfolio'),
    (N'Banking & Financial Services', N'Treasury', N'Trade Finance', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Architect', N'3D Visualization (SketchUp)', 2, N'P', N'Work history / Portfolio'),
    (N'Construction & Civil Engineering', N'Architect', N'ArchiCAD / Revit', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Architect', N'Architect License AZ', 2, N'R', N'Recognized certification only'),
    (N'Construction & Civil Engineering', N'Architect', N'Architectural Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Architect', N'BIM Modeling', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Architect', N'Interior Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Architect', N'Urban Planning basics', 2, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'Civil / Structural Engineer', N'Construction Supervision', 2, N'P', N'Work history / Portfolio'),
    (N'Construction & Civil Engineering', N'Civil / Structural Engineer', N'ETABS / SAP2000', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Civil / Structural Engineer', N'Foundation Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Civil / Structural Engineer', N'Reinforced Concrete Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Civil / Structural Engineer', N'Soil Investigation', 2, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'Civil / Structural Engineer', N'Structural Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'HSE Construction', N'HSE Plan Development', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'HSE Construction', N'Incident Reporting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'HSE Construction', N'OSHA / ISO 45001', 2, N'R', N'Recognized certification only'),
    (N'Construction & Civil Engineering', N'HSE Construction', N'PPE Compliance', 2, N'P', N'Work history / Portfolio'),
    (N'Construction & Civil Engineering', N'HSE Construction', N'Risk Assessment', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'MEP Engineer', N'BIM MEP', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'MEP Engineer', N'Electrical Systems', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'MEP Engineer', N'Fire Protection Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'MEP Engineer', N'HVAC Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'MEP Engineer', N'MEP Coordination', 2, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'MEP Engineer', N'Plumbing Systems', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Project Manager', N'Budget & Cost Control', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Project Manager', N'Claim Management', 2, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'Project Manager', N'Client Reporting', 2, N'P', N'Work history / Portfolio'),
    (N'Construction & Civil Engineering', N'Project Manager', N'Construction Scheduling', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Project Manager', N'PMP Certification', 2, N'R', N'Recognized certification only'),
    (N'Construction & Civil Engineering', N'Project Manager', N'Subcontractor Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Quantity Surveyor', N'Bill of Quantities', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Quantity Surveyor', N'Contract Administration', 2, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'Quantity Surveyor', N'Cost Control', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Quantity Surveyor', N'Cost Estimation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Construction & Civil Engineering', N'Quantity Surveyor', N'FIDIC Contracts', 2, N'T', N'Knowledge test / Certificate'),
    (N'Construction & Civil Engineering', N'Quantity Surveyor', N'Tender Evaluation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'3D Designer / Visualizer', N'3ds Max / Blender', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'3D Designer / Visualizer', N'Architectural Visualization', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'3D Designer / Visualizer', N'Lighting & Texturing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'3D Designer / Visualizer', N'SketchUp', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'3D Designer / Visualizer', N'V-Ray / Corona Renderer', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Art Director', N'Brand Strategy', 2, N'T', N'Knowledge test / Certificate'),
    (N'Creative & Graphic Design', N'Art Director', N'Campaign Art Direction', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Art Director', N'Client Presentation', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Art Director', N'Creative Concept Development', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Art Director', N'Team Creative Direction', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Graphic Designer', N'Banner / Ad Design', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Graphic Designer', N'Canva / Figma', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Graphic Designer', N'Digital / Social Media Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Graphic Designer', N'InDesign', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Graphic Designer', N'Packaging Design', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Graphic Designer', N'Print Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Motion Designer / Video Editor', N'Adobe Premiere Pro', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Motion Designer / Video Editor', N'After Effects', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Motion Designer / Video Editor', N'Audio Editing basics', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Motion Designer / Video Editor', N'Color Grading', 2, N'P', N'Work history / Portfolio'),
    (N'Creative & Graphic Design', N'Motion Designer / Video Editor', N'Motion Graphics', 2, N'TP', N'Knowledge test + Experience'),
    (N'Creative & Graphic Design', N'Motion Designer / Video Editor', N'Storyboarding', 2, N'T', N'Knowledge test / Certificate'),
    (N'Education & Training', N'Corporate Trainer', N'Adult Learning Principles', 2, N'T', N'Knowledge test / Certificate'),
    (N'Education & Training', N'Corporate Trainer', N'E-learning Tools', 2, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'Corporate Trainer', N'Facilitation Skills', 2, N'P', N'Work history / Portfolio'),
    (N'Education & Training', N'Corporate Trainer', N'Training Needs Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'Corporate Trainer', N'Training ROI Measurement', 2, N'T', N'Knowledge test / Certificate'),
    (N'Education & Training', N'Curriculum Developer', N'Bloom''s Taxonomy', 2, N'T', N'Knowledge test / Certificate'),
    (N'Education & Training', N'Curriculum Developer', N'Instructional Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'Curriculum Developer', N'Learning Objectives Writing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'Curriculum Developer', N'LMS Platforms', 2, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'Curriculum Developer', N'SCORM Content', 2, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'English Teacher', N'B2+ English Level', 2, N'R', N'Recognized certification only'),
    (N'Education & Training', N'English Teacher', N'Cambridge Curriculum', 2, N'T', N'Knowledge test / Certificate'),
    (N'Education & Training', N'English Teacher', N'CELTA / DELTA Certification', 2, N'R', N'Recognized certification only'),
    (N'Education & Training', N'English Teacher', N'IELTS / TOEFL Preparation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Education & Training', N'English Teacher', N'Interactive Teaching Methods', 2, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'Backend Developer', N'AWS / GCP / Azure', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Docker', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Go', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'GraphQL', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Java', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Message Queues (Kafka/RabbitMQ)', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Microservices Architecture', 2, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Backend Developer', N'Node.js', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'PostgreSQL', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Python', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Redis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'REST API Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'SQL', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'Supabase', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Backend Developer', N'System Design', 2, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Data Engineer', N'Airflow', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Data Engineer', N'Apache Spark', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Data Engineer', N'BigQuery / Redshift', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Data Engineer', N'Data Modeling', 2, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Data Engineer', N'dbt', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Data Engineer', N'ETL Pipelines', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Data Engineer', N'Python', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Data Engineer', N'SQL', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'DevOps Engineer', N'CI/CD Pipelines', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'DevOps Engineer', N'Docker', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'DevOps Engineer', N'Kubernetes', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'DevOps Engineer', N'Linux Administration', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'DevOps Engineer', N'Monitoring (Grafana/Prometheus)', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'DevOps Engineer', N'Shell Scripting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'DevOps Engineer', N'Terraform', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'Figma handoff', 2, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'Frontend Developer', N'HTML & CSS', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'JavaScript', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'Next.js', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'Performance Optimization', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'React', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'React Query / TanStack', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'Responsive Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'State Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'Tailwind CSS', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'TypeScript', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'Vite', 2, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'Frontend Developer', N'Vue.js', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Frontend Developer', N'Web Accessibility', 2, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Mobile Developer', N'App Store Deployment', 2, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'Mobile Developer', N'Flutter', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Mobile Developer', N'Kotlin', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Mobile Developer', N'React Native', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Mobile Developer', N'Swift / SwiftUI', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'QA Engineer', N'API Testing (Postman)', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'QA Engineer', N'Automated Testing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'QA Engineer', N'Bug Reporting', 2, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'QA Engineer', N'JIRA', 2, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'QA Engineer', N'Load Testing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'QA Engineer', N'Manual Testing', 2, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'QA Engineer', N'Playwright / Cypress', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'QA Engineer', N'Test Case Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Engineering (IT)', N'Senior / Lead Additions', N'Architecture Decision Records', 3, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Senior / Lead Additions', N'Mentoring', 3, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'Senior / Lead Additions', N'Roadmap Planning Tech', 3, N'P', N'Work history / Portfolio'),
    (N'Engineering (IT)', N'Senior / Lead Additions', N'System Design Advanced', 3, N'T', N'Knowledge test / Certificate'),
    (N'Engineering (IT)', N'Senior / Lead Additions', N'Technical Leadership', 3, N'P', N'Work history / Portfolio'),
    (N'Finance', N'Accountant', N'1C Accounting Software', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Accountant', N'Accounts Payable / Receivable', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Accountant', N'Audit Preparation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Accountant', N'Bank Reconciliation', 2, N'P', N'Work history / Portfolio'),
    (N'Finance', N'Accountant', N'Bookkeeping', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Accountant', N'IFRS / GAAP', 2, N'T', N'Knowledge test / Certificate'),
    (N'Finance', N'Accountant', N'Payroll Processing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Accountant', N'Tax Compliance', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'Audit Support', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'Cost Control', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'Financial Modeling', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'Forecasting & Budgeting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'KPI Reporting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'Power BI', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'Risk Assessment', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'SAP', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Financial Analyst / FP&A', N'Variance Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Senior / Lead', N'Investor Reporting', 3, N'TP', N'Knowledge test + Experience'),
    (N'Finance', N'Senior / Lead', N'M&A Analysis', 3, N'T', N'Knowledge test / Certificate'),
    (N'Finance', N'Senior / Lead', N'Strategic Financial Planning', 3, N'T', N'Knowledge test / Certificate'),
    (N'Finance', N'Senior / Lead', N'Team Management', 3, N'P', N'Work history / Portfolio'),
    (N'Finance', N'Senior / Lead', N'Treasury Management', 3, N'T', N'Knowledge test / Certificate'),
    (N'Hospitality & Tourism', N'Event Manager', N'Budget Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'Event Manager', N'Event Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'Event Manager', N'MICE Experience', 2, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Event Manager', N'On-site Management', 2, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Event Manager', N'Vendor Coordination', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'F&B Manager', N'Cost Control F&B', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'F&B Manager', N'Food Safety HACCP', 2, N'R', N'Recognized certification only'),
    (N'Hospitality & Tourism', N'F&B Manager', N'Menu Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'F&B Manager', N'POS Systems', 2, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'F&B Manager', N'Staff Scheduling', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'F&B Manager', N'Supplier Management', 2, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Front Office / Reservations', N'Check-in / Check-out Procedures', 2, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Front Office / Reservations', N'Reservation Systems', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'Front Office / Reservations', N'Revenue Management basics', 2, N'T', N'Knowledge test / Certificate'),
    (N'Hospitality & Tourism', N'Front Office / Reservations', N'Upselling Rooms', 2, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Revenue Manager', N'Competitor Rate Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'Revenue Manager', N'Forecasting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'Revenue Manager', N'OTA Management', 2, N'P', N'Work history / Portfolio'),
    (N'Hospitality & Tourism', N'Revenue Manager', N'Pricing Strategy', 2, N'TP', N'Knowledge test + Experience'),
    (N'Hospitality & Tourism', N'Revenue Manager', N'Revenue Management Systems', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Compliance Specialist', N'AML / KYC', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Compliance Specialist', N'Compliance Reporting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Compliance Specialist', N'GDPR / Data Protection', 2, N'T', N'Knowledge test / Certificate'),
    (N'Legal & Compliance', N'Compliance Specialist', N'Internal Audit Support', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Compliance Specialist', N'Policy Development', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Compliance Specialist', N'Regulatory Compliance', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Corporate Lawyer', N'Corporate Governance', 2, N'T', N'Knowledge test / Certificate'),
    (N'Legal & Compliance', N'Corporate Lawyer', N'Corporate Law AZ', 2, N'R', N'Recognized certification only'),
    (N'Legal & Compliance', N'Corporate Lawyer', N'Due Diligence', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Corporate Lawyer', N'M&A Legal Support', 2, N'T', N'Knowledge test / Certificate'),
    (N'Legal & Compliance', N'Corporate Lawyer', N'Notarization Procedures', 2, N'P', N'Work history / Portfolio'),
    (N'Legal & Compliance', N'Corporate Lawyer', N'Shareholder Agreements', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Labor / Employment Law', N'AZ Labor Code', 2, N'R', N'Recognized certification only'),
    (N'Legal & Compliance', N'Labor / Employment Law', N'Disciplinary Procedures', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Labor / Employment Law', N'Dispute Resolution', 2, N'T', N'Knowledge test / Certificate'),
    (N'Legal & Compliance', N'Labor / Employment Law', N'Employment Contracts', 2, N'TP', N'Knowledge test + Experience'),
    (N'Legal & Compliance', N'Labor / Employment Law', N'Termination Procedures AZ', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Customs Specialist', N'Azerbaijan Customs Regulations', 2, N'R', N'Recognized certification only'),
    (N'Logistics & Supply Chain', N'Customs Specialist', N'Customs Declaration Software', 2, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Customs Specialist', N'HS Code Classification', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Customs Specialist', N'Import / Export Procedures', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Customs Specialist', N'Incoterms', 2, N'T', N'Knowledge test / Certificate'),
    (N'Logistics & Supply Chain', N'Customs Specialist', N'Tariff & Duty Calculation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Logistics Specialist', N'Carrier Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Logistics Specialist', N'Fleet Tracking', 2, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Logistics Specialist', N'Freight Cost Optimization', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Logistics Specialist', N'Last-mile Delivery', 2, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Logistics Specialist', N'Route Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Logistics Specialist', N'Transport Regulations', 2, N'T', N'Knowledge test / Certificate'),
    (N'Logistics & Supply Chain', N'Logistics Specialist', N'Warehouse Management Systems', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Procurement', N'Contract Negotiation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Procurement', N'Cost Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Procurement', N'ERP Procurement Module', 2, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Procurement', N'RFQ / Tender Preparation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Procurement', N'Supplier Relationship Management', 2, N'P', N'Work history / Portfolio'),
    (N'Logistics & Supply Chain', N'Procurement', N'Vendor Sourcing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Supply Chain Manager', N'Demand Forecasting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Supply Chain Manager', N'ERP (SAP / 1C / Oracle)', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Supply Chain Manager', N'Inventory Optimization', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Supply Chain Manager', N'KPI Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Logistics & Supply Chain', N'Supply Chain Manager', N'Lean / Six Sigma', 2, N'T', N'Knowledge test / Certificate'),
    (N'Logistics & Supply Chain', N'Supply Chain Manager', N'S&OP Process', 2, N'T', N'Knowledge test / Certificate'),
    (N'Marketing', N'Brand / Content', N'Brand Identity', 2, N'T', N'Knowledge test / Certificate'),
    (N'Marketing', N'Brand / Content', N'Content Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Brand / Content', N'Editorial Calendar', 2, N'P', N'Work history / Portfolio'),
    (N'Marketing', N'Brand / Content', N'Long-form Writing', 2, N'P', N'Work history / Portfolio'),
    (N'Marketing', N'Digital Marketing', N'CRO', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Digital Marketing', N'Email Marketing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Digital Marketing', N'Funnel Optimization', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Digital Marketing', N'Google / Meta Ads', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Digital Marketing', N'Marketing Automation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'Digital Marketing', N'Performance Marketing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SEO Specialist', N'Core Web Vitals', 2, N'T', N'Knowledge test / Certificate'),
    (N'Marketing', N'SEO Specialist', N'Keyword Research', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SEO Specialist', N'Link Building', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SEO Specialist', N'On-page SEO', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SEO Specialist', N'SEMrush / Ahrefs', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SEO Specialist', N'Technical SEO', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SMM', N'Community Management', 2, N'P', N'Work history / Portfolio'),
    (N'Marketing', N'SMM', N'Content Creation', 2, N'P', N'Work history / Portfolio'),
    (N'Marketing', N'SMM', N'Influencer Collaboration', 2, N'P', N'Work history / Portfolio'),
    (N'Marketing', N'SMM', N'Social Media Analytics', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SMM', N'Social Media Strategy', 2, N'TP', N'Knowledge test + Experience'),
    (N'Marketing', N'SMM', N'Video Editing basic', 2, N'P', N'Work history / Portfolio'),
    (N'Oil, Gas & Energy', N'Drilling Engineer', N'BHA Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Drilling Engineer', N'Casing Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Drilling Engineer', N'Drilling Fluids', 2, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Drilling Engineer', N'Petrel / Landmark Software', 2, N'P', N'Work history / Portfolio'),
    (N'Oil, Gas & Energy', N'Drilling Engineer', N'Well Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Drilling Engineer', N'Wellbore Stability', 2, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'HSE', N'Emergency Response Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'HSE', N'Environmental Compliance', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'HSE', N'Incident Investigation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'HSE', N'ISO 14001 / ISO 45001', 2, N'R', N'Recognized certification only'),
    (N'Oil, Gas & Energy', N'HSE', N'NEBOSH / IOSH Certification', 2, N'R', N'Recognized certification only'),
    (N'Oil, Gas & Energy', N'HSE', N'Permit to Work Systems', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Mechanical / Instrument', N'Equipment Maintenance', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Mechanical / Instrument', N'PLC / SCADA basics', 2, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Mechanical / Instrument', N'Reliability Engineering', 2, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Mechanical / Instrument', N'Rotating Equipment', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Mechanical / Instrument', N'Vibration Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Process Engineer', N'Commissioning & Start-up', 2, N'P', N'Work history / Portfolio'),
    (N'Oil, Gas & Energy', N'Process Engineer', N'HAZOP / HAZID', 2, N'T', N'Knowledge test / Certificate'),
    (N'Oil, Gas & Energy', N'Process Engineer', N'Material Balances', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Process Engineer', N'P&ID Reading', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Process Engineer', N'Pressure Relief Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Process Engineer', N'Process Simulation (HYSYS/ASPEN)', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Reservoir Engineer', N'Eclipse / CMG Software', 2, N'P', N'Work history / Portfolio'),
    (N'Oil, Gas & Energy', N'Reservoir Engineer', N'Material Balance', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Reservoir Engineer', N'Production Forecasting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Reservoir Engineer', N'Reservoir Simulation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Oil, Gas & Energy', N'Reservoir Engineer', N'Volumetric Estimation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Administration', N'Business Communication', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Administration', N'Document Management', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Administration', N'Office Administration', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Administration', N'Procurement', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Administration', N'Scheduling & Coordination', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Program Manager', N'Cross-functional Leadership', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Program Manager', N'Executive Reporting', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Program Manager', N'Multi-project Coordination', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Program Manager', N'OKR Tracking', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Program Manager', N'Portfolio Management', 2, N'T', N'Knowledge test / Certificate'),
    (N'Operations', N'Program Manager', N'Vendor Management', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Project Manager', N'Agile / Scrum', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Project Manager', N'Budget Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Project Manager', N'JIRA / Asana / Trello', 2, N'P', N'Work history / Portfolio'),
    (N'Operations', N'Project Manager', N'PMP Certification', 2, N'R', N'Recognized certification only'),
    (N'Operations', N'Project Manager', N'Resource Allocation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Project Manager', N'Risk Register', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Project Manager', N'Scope Management', 2, N'T', N'Knowledge test / Certificate'),
    (N'Operations', N'Project Manager', N'Timeline Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Operations', N'Project Manager', N'Waterfall Methodology', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'HR Business Partner', N'Change Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'HR Business Partner', N'Engagement & Retention', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'HR Business Partner', N'HR Analytics', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'HR Business Partner', N'Organizational Design', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'HR Business Partner', N'Succession Planning', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'HR Business Partner', N'Talent Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'HR Business Partner', N'Workforce Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'HR Manager', N'Compensation & Benefits', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'HR Manager', N'Conflict Resolution', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'HR Manager', N'Employee Relations', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'HR Manager', N'HRMS Systems', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'HR Manager', N'Onboarding & Offboarding', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'HR Manager', N'Performance Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'HR Manager', N'Policy Development', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'L&D', N'Competency Frameworks', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'L&D', N'E-learning Development', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'L&D', N'Facilitation', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'L&D', N'LMS Administration', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'L&D', N'Needs Assessment', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'L&D', N'Training Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'Recruiter', N'Candidate Experience', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'Recruiter', N'Employer Branding', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'Recruiter', N'Hiring Metrics TTF/TTH/QoH', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'Recruiter', N'Job Description Writing', 2, N'TP', N'Knowledge test + Experience'),
    (N'People & HR', N'Recruiter', N'Offer Negotiation', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'Recruiter', N'Screening & Shortlisting', 2, N'P', N'Work history / Portfolio'),
    (N'People & HR', N'Recruiter', N'Skill-based Hiring', 2, N'T', N'Knowledge test / Certificate'),
    (N'People & HR', N'Recruiter', N'Sourcing LinkedIn / Boolean', 2, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Product Manager', N'A/B Testing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Product Manager', N'Competitor Analysis', 2, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Product Manager', N'Figma wireframing', 2, N'P', N'Work history / Portfolio'),
    (N'Product', N'Product Manager', N'Go-to-Market Planning', 2, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Product Manager', N'OKR Framework', 2, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Product Manager', N'PRD Writing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Product Manager', N'Product Discovery', 2, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Product Manager', N'Product Metrics', 2, N'TP', N'Knowledge test + Experience'),
    (N'Product', N'Product Manager', N'SQL basic analytics', 2, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Product Manager', N'User Interviews', 2, N'P', N'Work history / Portfolio'),
    (N'Product', N'Senior / Lead', N'Cross-functional Leadership', 3, N'P', N'Work history / Portfolio'),
    (N'Product', N'Senior / Lead', N'Platform Thinking', 3, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Senior / Lead', N'Product Strategy', 3, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Senior / Lead', N'Revenue Modeling', 3, N'T', N'Knowledge test / Certificate'),
    (N'Product', N'Senior / Lead', N'Vendor Management', 3, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Call Center Operator', N'CRM Ticketing (Salesforce/Zendesk)', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Call Center Operator', N'Escalation Handling', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Call Center Operator', N'Inbound / Outbound Handling', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Call Center Operator', N'Script Adherence', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Call Center Operator', N'Typing Speed 40+ WPM', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Customer Experience Manager', N'CX Strategy', 2, N'T', N'Knowledge test / Certificate'),
    (N'Retail & Customer Service', N'Customer Experience Manager', N'Journey Mapping', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Customer Experience Manager', N'NPS / CSAT Tracking', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Customer Experience Manager', N'Service Design', 2, N'T', N'Knowledge test / Certificate'),
    (N'Retail & Customer Service', N'Customer Experience Manager', N'Team Coaching', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Customer Experience Manager', N'Voice of Customer Programs', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Retail Manager', N'KPI Tracking (Sales/Conversion)', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Retail Manager', N'Loss Prevention', 2, N'T', N'Knowledge test / Certificate'),
    (N'Retail & Customer Service', N'Retail Manager', N'P&L Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Retail Manager', N'Staff Scheduling', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Retail Manager', N'Stock Management', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Sales Consultant / Advisor', N'Cash Handling', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Sales Consultant / Advisor', N'Consultative Selling', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Sales Consultant / Advisor', N'Inventory Awareness', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Sales Consultant / Advisor', N'POS Systems', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Sales Consultant / Advisor', N'Upselling / Cross-selling', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Visual Merchandiser', N'Brand Standards Compliance', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Visual Merchandiser', N'Photoshop / Illustrator basic', 2, N'P', N'Work history / Portfolio'),
    (N'Retail & Customer Service', N'Visual Merchandiser', N'Store Layout Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Retail & Customer Service', N'Visual Merchandiser', N'Window Display', 2, N'P', N'Work history / Portfolio'),
    (N'Sales', N'BDM', N'Competitive Intelligence', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'BDM', N'Go-to-Market Strategy', 2, N'T', N'Knowledge test / Certificate'),
    (N'Sales', N'BDM', N'Market Entry Strategy', 2, N'T', N'Knowledge test / Certificate'),
    (N'Sales', N'BDM', N'Partnership Development', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Key Account Manager', N'Customer Success', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Key Account Manager', N'Executive Communication', 2, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Key Account Manager', N'Relationship Management', 2, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Key Account Manager', N'SLA Management', 2, N'T', N'Knowledge test / Certificate'),
    (N'Sales', N'Key Account Manager', N'Upselling / Cross-selling', 2, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Sales Manager', N'Account Planning', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Sales Manager', N'B2B Sales', 2, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Sales Manager', N'Cold Outreach', 2, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Sales Manager', N'Contract Negotiation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Sales Manager', N'Demo & Presentation', 2, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Sales Manager', N'Lead Generation', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Sales Manager', N'Proposal Writing', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Sales Manager', N'Sales Forecasting', 2, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Senior / Lead', N'Revenue Forecasting', 3, N'TP', N'Knowledge test + Experience'),
    (N'Sales', N'Senior / Lead', N'Sales Strategy', 3, N'T', N'Knowledge test / Certificate'),
    (N'Sales', N'Senior / Lead', N'Team Coaching', 3, N'P', N'Work history / Portfolio'),
    (N'Sales', N'Senior / Lead', N'Territory Planning', 3, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'UI Designer', N'Dark Mode Design', 2, N'P', N'Work history / Portfolio'),
    (N'UX & Design', N'UI Designer', N'Design Tokens', 2, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'UI Designer', N'Developer Handoff', 2, N'P', N'Work history / Portfolio'),
    (N'UX & Design', N'UI Designer', N'Icon Design', 2, N'P', N'Work history / Portfolio'),
    (N'UX & Design', N'UI Designer', N'Motion Design', 2, N'P', N'Work history / Portfolio'),
    (N'UX & Design', N'UI Designer', N'UI Component Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'UX & Design', N'UX Designer', N'Accessibility WCAG', 2, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'UX Designer', N'Design Thinking', 2, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'UX Designer', N'Information Architecture', 2, N'T', N'Knowledge test / Certificate'),
    (N'UX & Design', N'UX Designer', N'Interaction Design', 2, N'TP', N'Knowledge test + Experience'),
    (N'UX & Design', N'UX Designer', N'Usability Testing', 2, N'P', N'Work history / Portfolio'),
    (N'UX & Design', N'UX Designer', N'User Journey Mapping', 2, N'TP', N'Knowledge test + Experience'),
    (N'UX & Design', N'UX Designer', N'User Research', 2, N'TP', N'Knowledge test + Experience');

    CREATE TABLE #GeneratedSkillProfiles
    (
        JobName NVARCHAR(150) NOT NULL,
        Descriptor NVARCHAR(100) NOT NULL,
        CONSTRAINT PK_GeneratedSkillProfiles PRIMARY KEY
            (JobName, Descriptor)
    );

    INSERT INTO #GeneratedSkillProfiles
    (
        JobName,
        Descriptor
    )
    VALUES
    (N'Engineering (IT)', N'System Architecture Design'),
    (N'Engineering (IT)', N'Secure Software Delivery'),
    (N'Engineering (IT)', N'Performance & Reliability Engineering'),
    (N'Engineering (IT)', N'Technical Debt Management'),
    (N'Engineering (IT)', N'Production Incident Analysis'),
    (N'Product', N'Product Discovery Research'),
    (N'Product', N'Roadmap Prioritization'),
    (N'Product', N'Product Analytics & Experimentation'),
    (N'Product', N'Requirements & Backlog Design'),
    (N'Product', N'Go-to-Market Coordination'),
    (N'UX & Design', N'User Research Synthesis'),
    (N'UX & Design', N'Interaction Design Systems'),
    (N'UX & Design', N'Accessibility Evaluation'),
    (N'UX & Design', N'Usability Testing'),
    (N'UX & Design', N'Design Handoff & QA'),
    (N'Finance', N'Financial Modeling & Scenario Analysis'),
    (N'Finance', N'Management Reporting'),
    (N'Finance', N'Budget Variance Analysis'),
    (N'Finance', N'Financial Controls'),
    (N'Finance', N'Cash Flow Planning'),
    (N'Sales', N'Opportunity Qualification'),
    (N'Sales', N'Commercial Proposal Development'),
    (N'Sales', N'Revenue Pipeline Analytics'),
    (N'Sales', N'Account Growth Planning'),
    (N'Sales', N'Sales Negotiation Strategy'),
    (N'Marketing', N'Campaign Planning & Attribution'),
    (N'Marketing', N'Audience Segmentation'),
    (N'Marketing', N'Marketing Analytics'),
    (N'Marketing', N'Conversion Optimization'),
    (N'Marketing', N'Brand Message Development'),
    (N'People & HR', N'Workforce Planning'),
    (N'People & HR', N'Competency-Based Interviewing'),
    (N'People & HR', N'HR Policy Application'),
    (N'People & HR', N'Employee Relations Casework'),
    (N'People & HR', N'People Analytics'),
    (N'Operations', N'Process Mapping & Optimization'),
    (N'Operations', N'Capacity Planning'),
    (N'Operations', N'Operational Risk Control'),
    (N'Operations', N'KPI Dashboarding'),
    (N'Operations', N'Cross-Functional Delivery'),
    (N'Banking & Financial Services', N'Credit Risk Assessment'),
    (N'Banking & Financial Services', N'Regulatory Control Testing'),
    (N'Banking & Financial Services', N'Financial Crime Detection'),
    (N'Banking & Financial Services', N'Portfolio Monitoring'),
    (N'Banking & Financial Services', N'Banking Product Operations'),
    (N'Logistics & Supply Chain', N'Inventory Flow Optimization'),
    (N'Logistics & Supply Chain', N'Transport Route Planning'),
    (N'Logistics & Supply Chain', N'Supplier Performance Analysis'),
    (N'Logistics & Supply Chain', N'Customs Documentation Control'),
    (N'Logistics & Supply Chain', N'Warehouse Capacity Planning'),
    (N'Retail & Customer Service', N'Customer Journey Optimization'),
    (N'Retail & Customer Service', N'Service Quality Monitoring'),
    (N'Retail & Customer Service', N'Retail Sales Analysis'),
    (N'Retail & Customer Service', N'Complaint Resolution Design'),
    (N'Retail & Customer Service', N'Store Operations Control'),
    (N'Oil, Gas & Energy', N'Process Safety Analysis'),
    (N'Oil, Gas & Energy', N'Permit-to-Work Control'),
    (N'Oil, Gas & Energy', N'Equipment Reliability Monitoring'),
    (N'Oil, Gas & Energy', N'Production Operations Planning'),
    (N'Oil, Gas & Energy', N'Technical Risk Assessment'),
    (N'Construction & Civil Engineering', N'Construction Method Planning'),
    (N'Construction & Civil Engineering', N'Cost & Quantity Control'),
    (N'Construction & Civil Engineering', N'Site Quality Inspection'),
    (N'Construction & Civil Engineering', N'Contract & Variation Management'),
    (N'Construction & Civil Engineering', N'Construction Safety Coordination'),
    (N'Legal & Compliance', N'Legal Research & Opinion Drafting'),
    (N'Legal & Compliance', N'Contract Risk Review'),
    (N'Legal & Compliance', N'Regulatory Compliance Monitoring'),
    (N'Legal & Compliance', N'Case & Evidence Management'),
    (N'Legal & Compliance', N'Corporate Governance Advisory'),
    (N'Administration & Office Management', N'Executive Calendar & Workflow Control'),
    (N'Administration & Office Management', N'Business Document Governance'),
    (N'Administration & Office Management', N'Vendor & Office Coordination'),
    (N'Administration & Office Management', N'Records Retention Management'),
    (N'Administration & Office Management', N'Administrative Process Design'),
    (N'Hospitality & Tourism', N'Guest Experience Operations'),
    (N'Hospitality & Tourism', N'Revenue & Occupancy Analysis'),
    (N'Hospitality & Tourism', N'Service Recovery Management'),
    (N'Hospitality & Tourism', N'Hospitality Quality Standards'),
    (N'Hospitality & Tourism', N'Event & Venue Coordination'),
    (N'Education & Training', N'Learning Outcome Design'),
    (N'Education & Training', N'Instructional Assessment'),
    (N'Education & Training', N'Classroom Facilitation'),
    (N'Education & Training', N'Curriculum Quality Review'),
    (N'Education & Training', N'Learner Progress Analytics'),
    (N'Creative & Graphic Design', N'Visual Concept Development'),
    (N'Creative & Graphic Design', N'Brand Asset Production'),
    (N'Creative & Graphic Design', N'Creative Quality Review'),
    (N'Creative & Graphic Design', N'Digital Content Optimization'),
    (N'Creative & Graphic Design', N'Production Workflow Management');

    CREATE TABLE #SourceSeniorities
    (
        SeniorityName NVARCHAR(50) NOT NULL PRIMARY KEY,
        SortOrder INT NOT NULL UNIQUE
    );

    INSERT INTO #SourceSeniorities (SeniorityName, SortOrder)
    VALUES
        (N'Junior', 1),
        (N'Middle', 2),
        (N'Senior', 3),
        (N'Lead', 4),
        (N'Head', 5);

    DECLARE @DynamicSortOrderBase INT =
    (
        SELECT MAX(candidate.SortOrderValue)
        FROM
        (
            SELECT ISNULL(MAX(SortOrder), 0) AS SortOrderValue FROM dbo.Seniorities
            UNION ALL
            SELECT MAX(SortOrder) FROM #SourceSeniorities
        ) AS candidate
    );

    ;WITH Missing AS
    (
        SELECT
            source.SeniorityName,
            source.SortOrder,
            ROW_NUMBER() OVER (ORDER BY source.SortOrder) AS MissingRowNumber
        FROM #SourceSeniorities AS source
        WHERE NOT EXISTS
        (
            SELECT 1 FROM dbo.Seniorities AS existing
            WHERE existing.[Name] = source.SeniorityName
        )
    )
    INSERT INTO dbo.Seniorities ([Name], SortOrder)
    SELECT
        missing.SeniorityName,
        CASE
            WHEN NOT EXISTS
                 (
                     SELECT 1 FROM dbo.Seniorities AS occupied
                     WHERE occupied.SortOrder = missing.SortOrder
                 )
                THEN missing.SortOrder
            ELSE @DynamicSortOrderBase + missing.MissingRowNumber
        END
    FROM Missing AS missing;

    INSERT INTO dbo.JobFamilies (JobName)
    SELECT DISTINCT source.JobName
    FROM #SourcePositions AS source
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.JobFamilies AS existing
        WHERE existing.JobName = source.JobName
    );

    INSERT INTO dbo.Positions (JobFamilyId, [Name])
    SELECT jobFamily.Id, source.PositionName
    FROM #SourcePositions AS source
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON jobFamily.JobName = source.JobName
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Positions AS existing
        WHERE existing.JobFamilyId = jobFamily.Id
          AND existing.[Name] = source.PositionName
    );

    -- Rebuild position availability for all workbook Job Families. Positions not
    -- present in the corrected source lose their links and disappear from selectors.
    DELETE link
    FROM dbo.PositionSeniorities AS link
    INNER JOIN dbo.Positions AS position
        ON position.Id = link.PositionId
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON jobFamily.Id = position.JobFamilyId
    WHERE EXISTS
    (
        SELECT 1 FROM #SourcePositions AS importedJob
        WHERE importedJob.JobName = jobFamily.JobName
    );

    INSERT INTO dbo.PositionSeniorities (PositionId, SeniorityId)
    SELECT position.Id, seniority.Id
    FROM #SourcePositions AS source
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON jobFamily.JobName = source.JobName
    INNER JOIN dbo.Positions AS position
        ON position.JobFamilyId = jobFamily.Id
       AND position.[Name] = source.PositionName
    INNER JOIN #SourceSeniorities AS sourceSeniority
        ON sourceSeniority.SortOrder >= source.MinimumSortOrder
    INNER JOIN dbo.Seniorities AS seniority
        ON seniority.[Name] = sourceSeniority.SeniorityName;

    CREATE TABLE #SourceSkills
    (
        JobName NVARCHAR(150) NOT NULL,
        PositionName NVARCHAR(150) NOT NULL,
        SkillName NVARCHAR(150) NOT NULL,
        MinimumSortOrder INT NOT NULL,
        IsCore BIT NOT NULL,
        AssessmentType NVARCHAR(10) NOT NULL,
        VerificationMethod NVARCHAR(120) NOT NULL
    );

    INSERT INTO #SourceSkills
    SELECT
        position.JobName,
        position.PositionName,
        core.SkillName,
        core.MinimumSortOrder,
        CAST(1 AS BIT),
        core.AssessmentType,
        core.VerificationMethod
    FROM #SourcePositions AS position
    INNER JOIN #CoreSkillDefinitions AS core
        ON core.JobName = position.JobName;

    INSERT INTO #SourceSkills
    SELECT
        definition.JobName,
        definition.PositionName,
        definition.SkillName,
        definition.MinimumSortOrder,
        CAST(0 AS BIT),
        definition.AssessmentType,
        definition.VerificationMethod
    FROM #PositionSkillDefinitions AS definition;

    INSERT INTO #SourceSkills
    SELECT
        position.JobName,
        position.PositionName,
        CONCAT(position.PositionName, N' — ', generated.Descriptor),
        position.MinimumSortOrder,
        CAST(0 AS BIT),
        N'TP',
        N'Knowledge test + Experience'
    FROM #SourcePositions AS position
    INNER JOIN #GeneratedSkillProfiles AS generated
        ON generated.JobName = position.JobName
    WHERE position.IsInAllPositions = 1;

    CREATE TABLE #EffectiveSkills
    (
        JobName NVARCHAR(150) NOT NULL,
        PositionName NVARCHAR(150) NOT NULL,
        SkillName NVARCHAR(150) NOT NULL,
        MinimumSortOrder INT NOT NULL,
        IsCore BIT NOT NULL,
        AssessmentType NVARCHAR(10) NOT NULL,
        VerificationMethod NVARCHAR(120) NOT NULL,
        CONSTRAINT PK_EffectiveSkills PRIMARY KEY
            (JobName, PositionName, SkillName)
    );

    INSERT INTO #EffectiveSkills
    SELECT
        source.JobName,
        source.PositionName,
        source.SkillName,
        MIN(source.MinimumSortOrder),
        CONVERT(BIT, MAX(CONVERT(INT, source.IsCore))),
        MAX(source.AssessmentType),
        MAX(source.VerificationMethod)
    FROM #SourceSkills AS source
    GROUP BY source.JobName, source.PositionName, source.SkillName;

    -- Preserve old Skill IDs for existing vacancies, but exclude stale skills from
    -- automatic selection and the manual active catalogue.
    UPDATE skill
    SET skill.IsActive = 0
    FROM dbo.Skills AS skill
    INNER JOIN dbo.Positions AS position
        ON position.Id = skill.PositionId
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON jobFamily.Id = position.JobFamilyId
    WHERE EXISTS
    (
        SELECT 1 FROM #SourcePositions AS importedJob
        WHERE importedJob.JobName = jobFamily.JobName
    );

    UPDATE skill
    SET
        skill.MinimumSenioritySortOrder = source.MinimumSortOrder,
        skill.IsCore = source.IsCore,
        skill.IsActive = 1,
        skill.AssessmentType = source.AssessmentType,
        skill.VerificationMethod = source.VerificationMethod
    FROM dbo.Skills AS skill
    INNER JOIN dbo.Positions AS position
        ON position.Id = skill.PositionId
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON jobFamily.Id = position.JobFamilyId
    INNER JOIN #EffectiveSkills AS source
        ON source.JobName = jobFamily.JobName
       AND source.PositionName = position.[Name]
       AND source.SkillName = skill.SkillName;

    INSERT INTO dbo.Skills
    (
        SkillName,
        PositionId,
        MinimumSenioritySortOrder,
        IsCore,
        IsActive,
        AssessmentType,
        VerificationMethod
    )
    SELECT
        source.SkillName,
        position.Id,
        source.MinimumSortOrder,
        source.IsCore,
        CAST(1 AS BIT),
        source.AssessmentType,
        source.VerificationMethod
    FROM #EffectiveSkills AS source
    INNER JOIN dbo.JobFamilies AS jobFamily
        ON jobFamily.JobName = source.JobName
    INNER JOIN dbo.Positions AS position
        ON position.JobFamilyId = jobFamily.Id
       AND position.[Name] = source.PositionName
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Skills AS existing
        WHERE existing.PositionId = position.Id
          AND existing.SkillName = source.SkillName
    );

    IF (SELECT COUNT(*) FROM #SourcePositions) <> 260
        THROW 51031, 'Source position count does not match the generated workbook manifest.', 1;

    IF (SELECT COUNT(*) FROM #EffectiveSkills) <> 3208
        THROW 51032, 'Source skill count does not match the generated workbook manifest.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM #SourcePositions AS source
        INNER JOIN dbo.JobFamilies AS jobFamily
            ON jobFamily.JobName = source.JobName
        INNER JOIN dbo.Positions AS position
            ON position.JobFamilyId = jobFamily.Id
           AND position.[Name] = source.PositionName
        CROSS APPLY
        (
            SELECT COUNT(*) AS ActualCount
            FROM dbo.PositionSeniorities AS link
            INNER JOIN dbo.Seniorities AS seniority
                ON seniority.Id = link.SeniorityId
            WHERE link.PositionId = position.Id
              AND seniority.[Name] IN (N'Junior', N'Middle', N'Senior', N'Lead', N'Head')
        ) AS coverage
        WHERE coverage.ActualCount <> (6 - source.MinimumSortOrder)
    )
        THROW 51033, 'Position seniority coverage verification failed.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.Skills AS skill
        INNER JOIN dbo.Positions AS position
            ON position.Id = skill.PositionId
        INNER JOIN dbo.JobFamilies AS jobFamily
            ON jobFamily.Id = position.JobFamilyId
        INNER JOIN #EffectiveSkills AS source
            ON source.JobName = jobFamily.JobName
           AND source.PositionName = position.[Name]
           AND source.SkillName = skill.SkillName
        WHERE skill.IsActive = 1
    ) <> 3208
        THROW 51034, 'Active skill verification failed.', 1;

    IF EXISTS
    (
        SELECT 1 FROM dbo.Skills
        WHERE SkillName = N'Core Skills' AND IsActive = 1
    )
        THROW 51035, 'Core Skills must remain a non-selectable section label.', 1;

    COMMIT TRANSACTION;

    SELECT
        (SELECT COUNT(*) FROM #SourcePositions) AS ImportedPositions,
        (SELECT COUNT(*) FROM #EffectiveSkills) AS ActivePositionSkillAssignments,
        (SELECT COUNT(*) FROM #CoreSkillDefinitions) AS CoreSkillDefinitions,
        (SELECT COUNT(*) FROM #PositionSkillDefinitions) AS WorkbookPositionSkillDefinitions;

    SELECT
        seniority.[Name],
        seniority.SortOrder,
        COUNT(link.PositionId) AS LinkedPositionCount
    FROM dbo.Seniorities AS seniority
    LEFT JOIN dbo.PositionSeniorities AS link
        ON link.SeniorityId = seniority.Id
    GROUP BY seniority.[Name], seniority.SortOrder
    ORDER BY seniority.SortOrder, seniority.[Name];
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
