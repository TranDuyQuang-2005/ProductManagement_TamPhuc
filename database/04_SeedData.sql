USE [ProductManagementDB];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = 'DM001')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, IsAdminProtected)
    VALUES ('DM001', N'Thịt', 'TH', 1, N'Các mặt hàng thịt và sản phẩm từ thịt', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = 'DM002')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, IsAdminProtected)
    VALUES ('DM002', N'Hải sản', 'HS', 1, N'Cá, tôm và các mặt hàng hải sản', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = 'DM003')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, IsAdminProtected)
    VALUES ('DM003', N'Rau củ', 'RC', 1, N'Rau, củ, quả tươi', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryCode = 'DM004')
    INSERT dbo.Categories(CategoryCode, CategoryName, CodePrefix, NextProductNumber, Description, IsActive, IsAdminProtected)
    VALUES ('DM004', N'Đồ uống', 'DU', 1, N'Các loại nước uống', 1, 1);

DECLARE @MeatId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = 'DM001');
DECLARE @SeafoodId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = 'DM002');
DECLARE @VegetableId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = 'DM003');
DECLARE @DrinkId INT = (SELECT Id FROM dbo.Categories WHERE CategoryCode = 'DM004');

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = 'TH000001')
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, StockQuantity, Description, IsActive, IsAdminProtected)
    VALUES ('TH000001', N'Thịt bò Mỹ', @MeatId, N'Kg', 350000, 0, N'Thịt bò Mỹ nhập khẩu', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = 'TH000002')
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, StockQuantity, Description, IsActive, IsAdminProtected)
    VALUES ('TH000002', N'Thịt heo', @MeatId, N'Kg', 145000, 0, N'Thịt heo tươi', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = 'HS000001')
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, StockQuantity, Description, IsActive, IsAdminProtected)
    VALUES ('HS000001', N'Cá hồi', @SeafoodId, N'Kg', 420000, 0, N'Cá hồi phi lê', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = 'RC000001')
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, StockQuantity, Description, IsActive, IsAdminProtected)
    VALUES ('RC000001', N'Cà rốt', @VegetableId, N'Kg', 32000, 0, N'Cà rốt tươi', 1, 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductCode = 'DU000001')
    INSERT dbo.Products(ProductCode, ProductName, CategoryId, Unit, Price, StockQuantity, Description, IsActive, IsAdminProtected)
    VALUES ('DU000001', N'Nước suối 500ml', @DrinkId, N'Chai', 7000, 0, N'Nước uống đóng chai', 1, 1);

UPDATE c
SET NextProductNumber =
    ISNULL((
        SELECT MAX(TRY_CONVERT(INT, RIGHT(p.ProductCode, 6))) + 1
        FROM dbo.Products p
        WHERE p.CategoryId = c.Id AND p.ProductCode LIKE c.CodePrefix + '[0-9][0-9][0-9][0-9][0-9][0-9]'
    ), 1)
FROM dbo.Categories c;

COMMIT TRANSACTION;
GO
