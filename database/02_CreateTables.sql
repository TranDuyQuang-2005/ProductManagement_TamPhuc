USE [ProductManagementDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id INT IDENTITY(1,1) NOT NULL,
        CategoryCode NVARCHAR(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        CategoryName NVARCHAR(200) NOT NULL,
        CodePrefix NVARCHAR(10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        NextProductNumber INT NOT NULL CONSTRAINT DF_Categories_NextProductNumber DEFAULT (1),
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
        CreatedByUserId NVARCHAR(128) NULL,
        CreatedByUsername NVARCHAR(256) NULL,
        CreatedByRole NVARCHAR(50) NULL,
        LastModifiedByUserId NVARCHAR(128) NULL,
        LastModifiedByUsername NVARCHAR(256) NULL,
        LastModifiedByRole NVARCHAR(50) NULL,
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(7) NULL,
        CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Categories_CategoryCode_NotBlank CHECK (LEN(LTRIM(RTRIM(CategoryCode))) > 0),
        CONSTRAINT CK_Categories_CategoryName_NotBlank CHECK (LEN(LTRIM(RTRIM(CategoryName))) > 0),
        CONSTRAINT CK_Categories_CodePrefix_Format CHECK (LEN(LTRIM(RTRIM(CodePrefix))) BETWEEN 2 AND 10 AND CodePrefix NOT LIKE '%[^A-Z0-9]%'),
        CONSTRAINT CK_Categories_NextProductNumber_Positive CHECK (NextProductNumber >= 1),
        CONSTRAINT CK_Categories_CreatedByRole_Valid CHECK (CreatedByRole IS NULL OR CreatedByRole IN (N'ADMIN', N'STAFF')),
        CONSTRAINT CK_Categories_LastModifiedByRole_Valid CHECK (LastModifiedByRole IS NULL OR LastModifiedByRole IN (N'ADMIN', N'STAFF'))
    );
END
GO


-- Upgrade-safe business columns for databases created by older project versions.
-- A clean database gets these columns in CREATE TABLE above; an existing Categories
-- table is upgraded here so 03_CreateIndexes.sql can safely index CodePrefix.
IF COL_LENGTH(N'dbo.Categories', N'CodePrefix') IS NULL
BEGIN
    ALTER TABLE dbo.Categories
        ADD CodePrefix NVARCHAR(10) COLLATE SQL_Latin1_General_CP1_CI_AS NULL;
END
GO

-- Keep the four original demo categories on the agreed business prefixes. Any other
-- legacy category receives a deterministic, unique alphanumeric prefix based on Id.
UPDATE dbo.Categories
SET CodePrefix = CASE CategoryCode
    WHEN N'DM001' THEN N'TH'
    WHEN N'DM002' THEN N'HS'
    WHEN N'DM003' THEN N'RC'
    WHEN N'DM004' THEN N'DU'
    ELSE N'C' + CONVERT(NVARCHAR(8), CONVERT(VARBINARY(4), Id), 2)
END
WHERE CodePrefix IS NULL OR LEN(LTRIM(RTRIM(CodePrefix))) = 0;
GO

UPDATE dbo.Categories
SET CodePrefix = UPPER(LTRIM(RTRIM(CodePrefix)));
GO

IF EXISTS
(
    SELECT 1
    FROM dbo.Categories
    WHERE LEN(CodePrefix) NOT BETWEEN 2 AND 10
       OR CodePrefix LIKE N'%[^A-Z0-9]%'
)
    THROW 51050, 'Categories.CodePrefix contains invalid legacy values. Prefix must be 2-10 uppercase letters/digits.', 1;
GO

IF EXISTS
(
    SELECT CodePrefix
    FROM dbo.Categories
    GROUP BY CodePrefix
    HAVING COUNT(*) > 1
)
    THROW 51051, 'Duplicate Categories.CodePrefix values exist. Resolve duplicates before continuing.', 1;
GO

ALTER TABLE dbo.Categories
    ALTER COLUMN CodePrefix NVARCHAR(10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL;
GO

IF COL_LENGTH(N'dbo.Categories', N'NextProductNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Categories
        ADD NextProductNumber INT NOT NULL
            CONSTRAINT DF_Categories_NextProductNumber DEFAULT (1) WITH VALUES;
END
GO

-- Repair a partially upgraded database where the counter column exists but contains
-- invalid/null values, then enforce the same shape as a clean database.
UPDATE dbo.Categories
SET NextProductNumber = 1
WHERE NextProductNumber IS NULL OR NextProductNumber < 1;
GO

ALTER TABLE dbo.Categories
    ALTER COLUMN NextProductNumber INT NOT NULL;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.object_id = dc.parent_object_id
       AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Categories')
      AND c.name = N'NextProductNumber'
)
BEGIN
    ALTER TABLE dbo.Categories
        ADD CONSTRAINT DF_Categories_NextProductNumber
            DEFAULT (1) FOR NextProductNumber;
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Categories_CodePrefix_Format'
      AND parent_object_id = OBJECT_ID(N'dbo.Categories')
)
BEGIN
    ALTER TABLE dbo.Categories WITH CHECK
        ADD CONSTRAINT CK_Categories_CodePrefix_Format
        CHECK (LEN(LTRIM(RTRIM(CodePrefix))) BETWEEN 2 AND 10
               AND CodePrefix NOT LIKE '%[^A-Z0-9]%');
END
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Categories_NextProductNumber_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Categories')
)
BEGIN
    ALTER TABLE dbo.Categories WITH CHECK
        ADD CONSTRAINT CK_Categories_NextProductNumber_Positive
        CHECK (NextProductNumber >= 1);
END
GO


-- Upgrade-safe columns for role-based category ownership. Internal updates to
-- NextProductNumber do not change LastModifiedByRole; only Category Update does.
IF COL_LENGTH(N'dbo.Categories', N'CreatedByUserId') IS NULL
    ALTER TABLE dbo.Categories ADD CreatedByUserId NVARCHAR(128) NULL;
GO
IF COL_LENGTH(N'dbo.Categories', N'CreatedByUsername') IS NULL
    ALTER TABLE dbo.Categories ADD CreatedByUsername NVARCHAR(256) NULL;
GO
IF COL_LENGTH(N'dbo.Categories', N'CreatedByRole') IS NULL
    ALTER TABLE dbo.Categories ADD CreatedByRole NVARCHAR(50) NULL;
GO
IF COL_LENGTH(N'dbo.Categories', N'LastModifiedByUserId') IS NULL
    ALTER TABLE dbo.Categories ADD LastModifiedByUserId NVARCHAR(128) NULL;
GO
IF COL_LENGTH(N'dbo.Categories', N'LastModifiedByUsername') IS NULL
    ALTER TABLE dbo.Categories ADD LastModifiedByUsername NVARCHAR(256) NULL;
GO
IF COL_LENGTH(N'dbo.Categories', N'LastModifiedByRole') IS NULL
    ALTER TABLE dbo.Categories ADD LastModifiedByRole NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Categories_CreatedByRole_Valid' AND parent_object_id = OBJECT_ID(N'dbo.Categories'))
    ALTER TABLE dbo.Categories WITH CHECK ADD CONSTRAINT CK_Categories_CreatedByRole_Valid CHECK (CreatedByRole IS NULL OR CreatedByRole IN (N'ADMIN', N'STAFF'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Categories_LastModifiedByRole_Valid' AND parent_object_id = OBJECT_ID(N'dbo.Categories'))
    ALTER TABLE dbo.Categories WITH CHECK ADD CONSTRAINT CK_Categories_LastModifiedByRole_Valid CHECK (LastModifiedByRole IS NULL OR LastModifiedByRole IN (N'ADMIN', N'STAFF'));
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id INT IDENTITY(1,1) NOT NULL,
        ProductCode NVARCHAR(50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        ProductName NVARCHAR(250) NOT NULL,
        CategoryId INT NOT NULL,
        Unit NVARCHAR(50) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        Quantity DECIMAL(18,2) NOT NULL,
        Description NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedByUserId NVARCHAR(128) NULL,
        CreatedByUsername NVARCHAR(256) NULL,
        CreatedByRole NVARCHAR(50) NULL,
        LastModifiedByUserId NVARCHAR(128) NULL,
        LastModifiedByUsername NVARCHAR(256) NULL,
        LastModifiedByRole NVARCHAR(50) NULL,
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(7) NULL,
        CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Products_ProductCode_NotBlank CHECK (LEN(LTRIM(RTRIM(ProductCode))) > 0),
        CONSTRAINT CK_Products_ProductName_NotBlank CHECK (LEN(LTRIM(RTRIM(ProductName))) > 0),
        CONSTRAINT CK_Products_Unit_NotBlank CHECK (LEN(LTRIM(RTRIM(Unit))) > 0),
        CONSTRAINT CK_Products_Price_NonNegative CHECK (Price >= 0),
        CONSTRAINT CK_Products_Quantity_NonNegative CHECK (Quantity >= 0),
        CONSTRAINT CK_Products_CreatedByRole_Valid CHECK (CreatedByRole IS NULL OR CreatedByRole IN (N'ADMIN', N'STAFF')),
        CONSTRAINT CK_Products_LastModifiedByRole_Valid CHECK (LastModifiedByRole IS NULL OR LastModifiedByRole IN (N'ADMIN', N'STAFF')),
        CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId)
            REFERENCES dbo.Categories(Id)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION
    );
END
GO

-- Upgrade-safe columns for role-based product ownership. These ALTERs make the script
-- safe to rerun against a database created by an earlier project version.
IF COL_LENGTH(N'dbo.Products', N'CreatedByUserId') IS NULL
    ALTER TABLE dbo.Products ADD CreatedByUserId NVARCHAR(128) NULL;
GO
IF COL_LENGTH(N'dbo.Products', N'CreatedByUsername') IS NULL
    ALTER TABLE dbo.Products ADD CreatedByUsername NVARCHAR(256) NULL;
GO
IF COL_LENGTH(N'dbo.Products', N'CreatedByRole') IS NULL
    ALTER TABLE dbo.Products ADD CreatedByRole NVARCHAR(50) NULL;
GO
IF COL_LENGTH(N'dbo.Products', N'LastModifiedByUserId') IS NULL
    ALTER TABLE dbo.Products ADD LastModifiedByUserId NVARCHAR(128) NULL;
GO
IF COL_LENGTH(N'dbo.Products', N'LastModifiedByUsername') IS NULL
    ALTER TABLE dbo.Products ADD LastModifiedByUsername NVARCHAR(256) NULL;
GO
IF COL_LENGTH(N'dbo.Products', N'LastModifiedByRole') IS NULL
    ALTER TABLE dbo.Products ADD LastModifiedByRole NVARCHAR(50) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Products_CreatedByRole_Valid' AND parent_object_id = OBJECT_ID(N'dbo.Products'))
    ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT CK_Products_CreatedByRole_Valid CHECK (CreatedByRole IS NULL OR CreatedByRole IN (N'ADMIN', N'STAFF'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Products_LastModifiedByRole_Valid' AND parent_object_id = OBJECT_ID(N'dbo.Products'))
    ALTER TABLE dbo.Products WITH CHECK ADD CONSTRAINT CK_Products_LastModifiedByRole_Valid CHECK (LastModifiedByRole IS NULL OR LastModifiedByRole IN (N'ADMIN', N'STAFF'));
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        UserId NVARCHAR(128) NULL,
        Username NVARCHAR(256) NOT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId NVARCHAR(100) NULL,
        EntityCode NVARCHAR(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        ChangedFields NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(64) NULL,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME2(7) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AuditLogs_Action_NotBlank CHECK (LEN(LTRIM(RTRIM(Action))) > 0),
        CONSTRAINT CK_AuditLogs_EntityType_NotBlank CHECK (LEN(LTRIM(RTRIM(EntityType))) > 0)
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoles
    (
        Id NVARCHAR(128) NOT NULL,
        Name NVARCHAR(256) NULL,
        NormalizedName NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetRoles PRIMARY KEY (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUsers
    (
        Id NVARCHAR(128) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        Role NVARCHAR(50) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAt DATETIME2(7) NOT NULL,
        LastLoginAt DATETIME2(7) NULL,
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL,
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL,
        TwoFactorEnabled BIT NOT NULL,
        LockoutEnd DATETIMEOFFSET(7) NULL,
        LockoutEnabled BIT NOT NULL,
        AccessFailedCount INT NOT NULL,
        CONSTRAINT PK_AspNetUsers PRIMARY KEY (Id)
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetRoleClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetRoleClaims
    (
        Id INT IDENTITY(1,1) NOT NULL,
        RoleId NVARCHAR(128) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetRoleClaims PRIMARY KEY (Id),
        CONSTRAINT FK_AspNetRoleClaims_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserClaims
    (
        Id INT IDENTITY(1,1) NOT NULL,
        UserId NVARCHAR(128) NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserClaims PRIMARY KEY (Id),
        CONSTRAINT FK_AspNetUserClaims_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserLogins
    (
        LoginProvider NVARCHAR(128) NOT NULL,
        ProviderKey NVARCHAR(128) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId NVARCHAR(128) NOT NULL,
        CONSTRAINT PK_AspNetUserLogins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_AspNetUserLogins_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserRoles
    (
        UserId NVARCHAR(128) NOT NULL,
        RoleId NVARCHAR(128) NOT NULL,
        CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_AspNetUserRoles_AspNetRoles_RoleId FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_AspNetUserRoles_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AspNetUserTokens
    (
        UserId NVARCHAR(128) NOT NULL,
        LoginProvider NVARCHAR(128) NOT NULL,
        Name NVARCHAR(128) NOT NULL,
        Value NVARCHAR(MAX) NULL,
        CONSTRAINT PK_AspNetUserTokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_AspNetUserTokens_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE CASCADE
    );
END
GO
