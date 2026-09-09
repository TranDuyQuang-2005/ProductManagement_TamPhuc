USE [ProductManagementDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = N'DM001')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'DM001', N'Thịt', N'TH', 1, N'Các mặt hàng thịt và sản phẩm từ thịt', 1, N'system-admin-seed', N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = N'DM002')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'DM002', N'Hải sản', N'HS', 1, N'Cá, tôm và các mặt hàng hải sản', 1, N'system-admin-seed', N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = N'DM003')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'DM003', N'Rau củ', N'RC', 1, N'Rau, củ, quả tươi', 1, N'system-admin-seed', N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = N'DM004')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'DM004', N'Đồ uống', N'DU', 1, N'Các loại nước uống', 1, N'system-admin-seed', N'ADMIN');


UPDATE dbo.Categories
SET CreatedByUsername = COALESCE(CreatedByUsername, N'system-admin-seed'),
    CreatedByRole = COALESCE(CreatedByRole, N'ADMIN')
WHERE CreatedByRole IS NULL;

DECLARE @MeatId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = N'DM001');
DECLARE @SeafoodId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = N'DM002');
DECLARE @VegetableId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = N'DM003');
DECLARE @DrinkId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = N'DM004');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = N'TH000001' OR (CategoryId = @MeatId AND ProductName = N'Thịt bò Mỹ'))
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, Quantity, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'TH000001', N'Thịt bò Mỹ', @MeatId, N'Kg', 350000, 30, N'Thịt bò Mỹ nhập khẩu', 1, N'system-admin-seed', N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = N'TH000002' OR (CategoryId = @MeatId AND ProductName = N'Thịt heo'))
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, Quantity, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'TH000002', N'Thịt heo', @MeatId, N'Kg', 145000, 50, N'Thịt heo tươi', 1, N'system-admin-seed', N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = N'HS000001' OR (CategoryId = @SeafoodId AND ProductName = N'Cá hồi'))
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, Quantity, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'HS000001', N'Cá hồi', @SeafoodId, N'Kg', 420000, 15, N'Cá hồi phi lê', 1, N'system-admin-seed', N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = N'RC000001' OR (CategoryId = @VegetableId AND ProductName = N'Cà rốt'))
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, Quantity, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'RC000001', N'Cà rốt', @VegetableId, N'Kg', 32000, 0, N'Cà rốt tươi - dữ liệu mẫu hết hàng', 1, N'system-admin-seed', N'ADMIN');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = N'DU000001' OR (CategoryId = @DrinkId AND ProductName = N'Nước suối 500ml'))
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, Quantity, Description, IsActive, CreatedByUsername, CreatedByRole)
    VALUES (N'DU000001', N'Nước suối 500ml', @DrinkId, N'Chai', 7000, 120, N'Nước uống đóng chai', 1, N'system-admin-seed', N'ADMIN');

UPDATE dbo.Products
SET CreatedByUsername = COALESCE(CreatedByUsername, N'system-admin-seed'),
    CreatedByRole = COALESCE(CreatedByRole, N'ADMIN')
WHERE CreatedByRole IS NULL;

UPDATE c
SET NextProductNumber =
    ISNULL((
        SELECT MAX(TRY_CONVERT(INT, RIGHT(p.ProductCode, 6))) + 1
        FROM dbo.Products p
        WHERE p.CategoryId = c.Id AND p.ProductCode LIKE c.CodePrefix + N'[0-9][0-9][0-9][0-9][0-9][0-9]'
    ), 1)
FROM dbo.Categories c;

COMMIT TRANSACTION;
GO
