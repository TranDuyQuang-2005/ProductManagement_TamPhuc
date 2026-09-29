USE [ProductManagementDB];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
        IsActive BIT NOT NULL CONSTRAINT DF_AspNetUsers_IsActive DEFAULT (1),
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_AspNetUsers_CreatedAt DEFAULT (SYSUTCDATETIME()),
        LastLoginAt DATETIME2(3) NULL,
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

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        Id INT IDENTITY(1,1) NOT NULL,
        CategoryCode VARCHAR(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        CategoryName NVARCHAR(200) NOT NULL,
        CodePrefix VARCHAR(10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        NextProductNumber INT NOT NULL CONSTRAINT DF_Categories_NextProductNumber DEFAULT (1),
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Categories_IsActive DEFAULT (1),
        CreatedByUserId NVARCHAR(128) NULL,
        LastModifiedByUserId NVARCHAR(128) NULL,
        IsAdminProtected BIT NOT NULL CONSTRAINT DF_Categories_IsAdminProtected DEFAULT (0),
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(3) NULL,
        CONSTRAINT PK_Categories PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Categories_CategoryCode_NotBlank CHECK (LEN(LTRIM(RTRIM(CategoryCode))) > 0),
        CONSTRAINT CK_Categories_CategoryName_NotBlank CHECK (LEN(LTRIM(RTRIM(CategoryName))) > 0),
        CONSTRAINT CK_Categories_CodePrefix_Format CHECK (LEN(LTRIM(RTRIM(CodePrefix))) BETWEEN 2 AND 10 AND CodePrefix NOT LIKE '%[^A-Z0-9]%'),
        CONSTRAINT CK_Categories_NextProductNumber_Positive CHECK (NextProductNumber >= 1),
        CONSTRAINT FK_Categories_AspNetUsers_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Categories_AspNetUsers_LastModifiedByUserId FOREIGN KEY (LastModifiedByUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION
    );
END
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id INT IDENTITY(1,1) NOT NULL,
        ProductCode VARCHAR(20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
        ProductName NVARCHAR(250) NOT NULL,
        CategoryId INT NOT NULL,
        Unit NVARCHAR(50) NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        StockQuantity DECIMAL(18,2) NOT NULL CONSTRAINT DF_Products_StockQuantity DEFAULT (0),
        Description NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedByUserId NVARCHAR(128) NULL,
        LastModifiedByUserId NVARCHAR(128) NULL,
        IsAdminProtected BIT NOT NULL CONSTRAINT DF_Products_IsAdminProtected DEFAULT (0),
        IsDeleted BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT (0),
        DeletedAt DATETIME2(3) NULL,
        DeletedByUserId NVARCHAR(128) NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(3) NULL,
        CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Products_ProductCode_NotBlank CHECK (LEN(LTRIM(RTRIM(ProductCode))) > 0),
        CONSTRAINT CK_Products_ProductName_NotBlank CHECK (LEN(LTRIM(RTRIM(ProductName))) > 0),
        CONSTRAINT CK_Products_Unit_NotBlank CHECK (LEN(LTRIM(RTRIM(Unit))) > 0),
        CONSTRAINT CK_Products_Price_NonNegative CHECK (Price >= 0),
        CONSTRAINT CK_Products_StockQuantity_NonNegative CHECK (StockQuantity >= 0),
        CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Products_AspNetUsers_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Products_AspNetUsers_LastModifiedByUserId FOREIGN KEY (LastModifiedByUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Products_AspNetUsers_DeletedByUserId FOREIGN KEY (DeletedByUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION
    );
END
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        UserId NVARCHAR(128) NULL,
        Username NVARCHAR(256) NOT NULL,
        Action VARCHAR(30) NOT NULL,
        EntityType VARCHAR(30) NOT NULL,
        EntityId VARCHAR(128) NULL,
        EntityCode VARCHAR(100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        ChangedFields NVARCHAR(MAX) NULL,
        IpAddress VARCHAR(45) NULL,
        Description NVARCHAR(500) NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AuditLogs_Action_NotBlank CHECK (LEN(LTRIM(RTRIM(Action))) > 0),
        CONSTRAINT CK_AuditLogs_EntityType_NotBlank CHECK (LEN(LTRIM(RTRIM(EntityType))) > 0),
        CONSTRAINT FK_AuditLogs_AspNetUsers_UserId FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION
    );
END
GO

IF OBJECT_ID(N'dbo.InventoryTransactions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryTransactions
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        ProductId INT NOT NULL,
        MovementType VARCHAR(20) NOT NULL,
        QuantityChange DECIMAL(18,2) NOT NULL,
        QuantityBefore DECIMAL(18,2) NOT NULL,
        QuantityAfter DECIMAL(18,2) NOT NULL,
        ReferenceCode VARCHAR(50) NULL,
        Note NVARCHAR(500) NULL,
        CreatedByUserId NVARCHAR(128) NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_InventoryTransactions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_InventoryTransactions PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_InventoryTransactions_Products_ProductId FOREIGN KEY (ProductId) REFERENCES dbo.Products(Id) ON DELETE NO ACTION,
        CONSTRAINT FK_InventoryTransactions_AspNetUsers_CreatedByUserId FOREIGN KEY (CreatedByUserId) REFERENCES dbo.AspNetUsers(Id) ON DELETE NO ACTION,
        CONSTRAINT CK_InventoryTransactions_QuantityBefore_NonNegative CHECK (QuantityBefore >= 0),
        CONSTRAINT CK_InventoryTransactions_QuantityAfter_NonNegative CHECK (QuantityAfter >= 0),
        CONSTRAINT CK_InventoryTransactions_QuantityChange_NotZero CHECK (QuantityChange <> 0)
    );
END
GO
