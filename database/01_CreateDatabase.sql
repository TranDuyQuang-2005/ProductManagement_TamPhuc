USE [master];
GO

IF DB_ID(N'ProductManagementDB') IS NULL
BEGIN
    CREATE DATABASE [ProductManagementDB];
END
GO

ALTER DATABASE [ProductManagementDB] SET RECOVERY SIMPLE;
GO
