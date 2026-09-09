USE [ProductManagementDB];
GO

IF COL_LENGTH(N'dbo.Categories', N'CodePrefix') IS NULL
    THROW 51100, 'Missing Categories.CodePrefix. Run the corrected 02_CreateTables.sql before 03_CreateIndexes.sql.', 1;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_CategoryCode' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Categories_CategoryCode ON dbo.Categories(CategoryCode);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Categories_CodePrefix' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Categories_CodePrefix ON dbo.Categories(CodePrefix);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Categories_CategoryName' AND object_id = OBJECT_ID(N'dbo.Categories'))
    CREATE NONCLUSTERED INDEX IX_Categories_CategoryName ON dbo.Categories(CategoryName);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Products_ProductCode' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Products_ProductCode ON dbo.Products(ProductCode);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_ProductName' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE NONCLUSTERED INDEX IX_Products_ProductName ON dbo.Products(ProductName);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CreatedAt' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_UserId' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_UserId ON dbo.AuditLogs(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_EntityType_EntityId' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_EntityType_EntityId ON dbo.AuditLogs(EntityType, EntityId, CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_EntityCode' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_EntityCode ON dbo.AuditLogs(EntityCode, CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_Action' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE NONCLUSTERED INDEX IX_AuditLogs_Action ON dbo.AuditLogs(Action, CreatedAt DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'EmailIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE NONCLUSTERED INDEX EmailIndex ON dbo.AspNetUsers(NormalizedEmail);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UserNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetUsers'))
    CREATE UNIQUE NONCLUSTERED INDEX UserNameIndex ON dbo.AspNetUsers(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'RoleNameIndex' AND object_id = OBJECT_ID(N'dbo.AspNetRoles'))
    CREATE UNIQUE NONCLUSTERED INDEX RoleNameIndex ON dbo.AspNetRoles(NormalizedName) WHERE NormalizedName IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetRoleClaims_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetRoleClaims'))
    CREATE NONCLUSTERED INDEX IX_AspNetRoleClaims_RoleId ON dbo.AspNetRoleClaims(RoleId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserClaims_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserClaims'))
    CREATE NONCLUSTERED INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserLogins_UserId' AND object_id = OBJECT_ID(N'dbo.AspNetUserLogins'))
    CREATE NONCLUSTERED INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AspNetUserRoles_RoleId' AND object_id = OBJECT_ID(N'dbo.AspNetUserRoles'))
    CREATE NONCLUSTERED INDEX IX_AspNetUserRoles_RoleId ON dbo.AspNetUserRoles(RoleId);
GO
