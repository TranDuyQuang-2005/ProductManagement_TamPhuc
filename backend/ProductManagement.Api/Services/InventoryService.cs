using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.Inventory;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Services;

public sealed class InventoryService(
    AppDbContext dbContext,
    DapperContext dapperContext,
    ICurrentUserService currentUser,
    IAuditService auditService) : IInventoryService
{
    private const string StockInMovementType = "STOCK_IN";

    public async Task<InventoryTransactionResponse> StockInAsync(StockInRequest request, CancellationToken cancellationToken)
    {
        var quantity = ValidateStockInQuantity(request.Quantity);
        var referenceCode = NormalizeReferenceCode(request.ReferenceCode);
        var note = NormalizeOptionalText(request.Note);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var product = await dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {request.ProductId} AND IsDeleted = 0")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound("Khong tim thay hang hoa.");

        if (!RecordAccessPolicy.CanModify(currentUser.Role, product.IsAdminProtected))
            throw AppException.Forbidden("Ban khong co quyen nhap hang cho hang hoa nay.");

        var category = await dbContext.Categories
            .AsNoTracking()
            .Where(x => x.Id == product.CategoryId)
            .Select(x => new { x.CategoryCode, x.CategoryName })
            .SingleOrDefaultAsync(cancellationToken);

        var oldSnapshot = ProductService.ProductSnapshot(product, category?.CategoryCode, category?.CategoryName);
        var before = product.StockQuantity;
        var after = before + quantity;
        product.StockQuantity = after;
        product.LastModifiedByUserId = currentUser.UserId;
        product.UpdatedAt = DateTime.UtcNow;

        var inventoryTransaction = new InventoryTransaction
        {
            ProductId = product.Id,
            MovementType = StockInMovementType,
            QuantityChange = quantity,
            QuantityBefore = before,
            QuantityAfter = after,
            ReferenceCode = referenceCode,
            Note = note,
            CreatedByUserId = currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await dbContext.InventoryTransactions.AddAsync(inventoryTransaction, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var newSnapshot = ProductService.ProductSnapshot(product, category?.CategoryCode, category?.CategoryName);
        await auditService.AddAsync(
            "STOCK_IN",
            "PRODUCT",
            product.Id.ToString(),
            product.ProductCode,
            oldSnapshot,
            newSnapshot,
            [nameof(Product.StockQuantity)],
            $"Da nhap them {quantity:0.00} don vi cho hang hoa {product.ProductCode}.",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetTransactionByIdAsync(inventoryTransaction.Id, cancellationToken)
               ?? throw AppException.NotFound("Khong tim thay giao dich kho vua tao.");
    }

    public async Task<PagedResult<InventoryTransactionResponse>> GetHistoryAsync(InventoryHistoryRequest request, CancellationToken cancellationToken)
    {
        if (!await dbContext.Products.IgnoreQueryFilters().AnyAsync(x => x.Id == request.ProductId, cancellationToken))
            throw AppException.NotFound("Khong tim thay hang hoa.");

        const string sql = """
            SELECT
                it.Id,
                it.ProductId,
                it.MovementType,
                it.QuantityChange,
                it.QuantityBefore,
                it.QuantityAfter,
                it.ReferenceCode,
                it.Note,
                it.CreatedByUserId,
                u.UserName AS Username,
                it.CreatedAt
            FROM dbo.InventoryTransactions it
            LEFT JOIN dbo.AspNetUsers u ON u.Id = it.CreatedByUserId
            WHERE it.ProductId = @ProductId
            ORDER BY it.CreatedAt DESC, it.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.InventoryTransactions it
            WHERE it.ProductId = @ProductId;
            """;

        await using var connection = dapperContext.CreateConnection();
        var command = new CommandDefinition(sql, new
        {
            request.ProductId,
            Offset = (request.Page - 1) * request.PageSize,
            request.PageSize
        }, cancellationToken: cancellationToken);
        using var grid = await connection.QueryMultipleAsync(command);
        var items = (await grid.ReadAsync<InventoryTransactionResponse>()).AsList();
        var totalItems = await grid.ReadSingleAsync<int>();
        return PagedResult<InventoryTransactionResponse>.Create(items, request.Page, request.PageSize, totalItems);
    }

    private async Task<InventoryTransactionResponse?> GetTransactionByIdAsync(long id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                it.Id,
                it.ProductId,
                it.MovementType,
                it.QuantityChange,
                it.QuantityBefore,
                it.QuantityAfter,
                it.ReferenceCode,
                it.Note,
                it.CreatedByUserId,
                u.UserName AS Username,
                it.CreatedAt
            FROM dbo.InventoryTransactions it
            LEFT JOIN dbo.AspNetUsers u ON u.Id = it.CreatedByUserId
            WHERE it.Id = @Id;
            """;

        await using var connection = dapperContext.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<InventoryTransactionResponse>(command);
    }

    private static decimal ValidateStockInQuantity(decimal? value)
    {
        const decimal maxDecimal18_2 = 9_999_999_999_999_999.99m;

        if (!value.HasValue)
            throw AppException.BadRequest("So luong nhap la bat buoc.", "quantity", "Khong duoc de trong.");

        if (value.Value <= 0 || value.Value > maxDecimal18_2)
            throw AppException.BadRequest("So luong nhap phai lon hon 0 va trong pham vi cho phep.", "quantity", "So luong nhap phai lon hon 0.");

        if (decimal.Round(value.Value, 2) != value.Value)
            throw AppException.BadRequest("So luong nhap chi duoc co toi da 2 chu so thap phan.", "quantity", "Chi duoc nhap toi da 2 chu so sau dau thap phan.");

        return value.Value;
    }

    private static string? NormalizeReferenceCode(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
