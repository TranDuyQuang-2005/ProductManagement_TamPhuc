using System.Data;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.DTOs.Products;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Repositories.Interfaces;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Services;

public sealed class ProductService(
    IProductQueryRepository queryRepository,
    IProductCommandRepository commandRepository,
    ICategoryQueryRepository categoryQueryRepository,
    IAuditLogQueryRepository auditLogQueryRepository,
    IAuditService auditService,
    ICurrentUserService currentUser,
    AppDbContext dbContext) : IProductService
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "productCode", "productName", "categoryName", "price", "quantity", "createdAt"
    };

    public async Task<PagedResult<ProductResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        ValidateSearchRequest(request);
        return await queryRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
        => await queryRepository.GetByIdAsync(id, cancellationToken)
           ?? throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

    public async Task<ProductResponse> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken)
    {
        var productName = NormalizeRequiredText(request.ProductName, "productName", "Ten hang hoa");
        var unit = NormalizeRequiredText(request.Unit, "unit", "Don vi tinh");
        var description = NormalizeOptionalText(request.Description);
        var price = ValidateAmount(request.Price, "price", "Gia ban");
        var quantity = ValidateAmount(request.Quantity, "quantity", "So luong");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var category = await dbContext.Categories
            .FromSqlInterpolated($"SELECT * FROM dbo.Categories WITH (UPDLOCK, ROWLOCK) WHERE Id = {request.CategoryId}")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.BadRequest("Danh muc duoc chon khong ton tai.", "categoryId", "Danh muc khong ton tai.");

        if (!category.IsActive)
        {
            throw AppException.BadRequest("Khong the gan hang hoa vao danh muc dang ngung hoat dong.", "categoryId", "Danh muc dang ngung hoat dong.");
        }

        var productCode = $"{category.CodePrefix}{category.NextProductNumber:000000}";
        category.NextProductNumber++;

        var entity = new Product
        {
            ProductCode = productCode,
            ProductName = productName,
            CategoryId = category.Id,
            Unit = unit,
            Price = price,
            Quantity = quantity,
            Description = description,
            IsActive = request.IsActive,
            CreatedByUserId = currentUser.UserId,
            CreatedByUsername = currentUser.Username,
            CreatedByRole = NormalizeRole(currentUser.Role),
            CreatedAt = DateTime.UtcNow
        };

        await commandRepository.AddAsync(entity, cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await auditService.AddAsync(
            "CREATE",
            "PRODUCT",
            entity.Id.ToString(),
            entity.ProductCode,
            null,
            ProductSnapshot(entity, category.CategoryCode, category.CategoryName),
            null,
            $"Created product {entity.ProductCode}.",
            cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<ProductResponse> UpdateAsync(int id, ProductUpdateRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var entity = await dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

        RecordAccessPolicy.EnsureCanModify(
            currentUser.Role, entity.CreatedByRole, entity.LastModifiedByRole, "Hang hoa");

        var productName = NormalizeRequiredText(request.ProductName, "productName", "Ten hang hoa");
        var unit = NormalizeRequiredText(request.Unit, "unit", "Don vi tinh");
        var description = NormalizeOptionalText(request.Description);
        var price = ValidateAmount(request.Price, "price", "Gia ban");
        var quantity = ValidateAmount(request.Quantity, "quantity", "So luong");
        var category = await categoryQueryRepository.GetOptionByIdAsync(entity.CategoryId, cancellationToken);
        var oldSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);

        entity.ProductName = productName;
        entity.Unit = unit;
        entity.Price = price;
        entity.Quantity = quantity;
        entity.Description = description;
        entity.IsActive = request.IsActive!.Value;
        entity.LastModifiedByUserId = currentUser.UserId;
        entity.LastModifiedByUsername = currentUser.Username;
        entity.LastModifiedByRole = NormalizeRole(currentUser.Role);
        entity.UpdatedAt = DateTime.UtcNow;

        await commandRepository.SaveChangesAsync(cancellationToken);
        var newSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);
        var changedFields = GetChangedProductFields(oldSnapshot, newSnapshot);
        await auditService.AddAsync(
            "UPDATE",
            "PRODUCT",
            entity.Id.ToString(),
            entity.ProductCode,
            oldSnapshot,
            newSnapshot,
            changedFields,
            $"Updated product {entity.ProductCode}.",
            cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var entity = await dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

        RecordAccessPolicy.EnsureCanModify(
            currentUser.Role, entity.CreatedByRole, entity.LastModifiedByRole, "Hang hoa");

        var category = await categoryQueryRepository.GetOptionByIdAsync(entity.CategoryId, cancellationToken);
        var oldSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);
        commandRepository.Remove(entity);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await auditService.AddAsync(
            "DELETE",
            "PRODUCT",
            entity.Id.ToString(),
            entity.ProductCode,
            oldSnapshot,
            null,
            null,
            $"Deleted product {entity.ProductCode}.",
            cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogResponse>> GetHistoryAsync(int id, int page, int pageSize, CancellationToken cancellationToken)
    {
        _ = await queryRepository.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

        return await auditLogQueryRepository.SearchAsync(new AuditLogSearchRequest
        {
            EntityType = "PRODUCT",
            EntityId = id.ToString(),
            Page = page,
            PageSize = pageSize,
            SortBy = "createdAt",
            SortDirection = "desc"
        }, cancellationToken);
    }

    private static string NormalizeRole(string? role)
        => string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim().ToUpperInvariant();

    private static void ValidateSearchRequest(ProductSearchRequest request)
    {
        if (!Enum.IsDefined(request.StockStatus))
            throw AppException.BadRequest("Trang thai ton kho khong hop le.", "stockStatus", "Chi chap nhan All, InStock hoac OutOfStock.");

        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
            throw AppException.BadRequest("Gia tu khong duoc lon hon gia den.", "minPrice", "Gia tu phai nho hon hoac bang gia den.");

        if (!AllowedSortFields.Contains(request.SortBy))
            throw AppException.BadRequest("Truong sap xep khong hop le.", "sortBy", $"Chi ho tro: {string.Join(", ", AllowedSortFields)}.");

        if (!string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw AppException.BadRequest("Chieu sap xep khong hop le.", "sortDirection", "Chi chap nhan 'asc' hoac 'desc'.");
    }

    private static decimal ValidateAmount(decimal? value, string field, string label)
    {
        const decimal maxDecimal18_2 = 9_999_999_999_999_999.99m;

        if (!value.HasValue)
            throw AppException.BadRequest($"{label} la bat buoc.", field, "Khong duoc de trong.");

        if (value.Value < 0 || value.Value > maxDecimal18_2)
            throw AppException.BadRequest($"{label} nam ngoai pham vi cho phep.", field, $"Gia tri phai tu 0 den {maxDecimal18_2}.");

        if (decimal.Round(value.Value, 2) != value.Value)
            throw AppException.BadRequest($"{label} chi duoc co toi da 2 chu so thap phan.", field, "Chi duoc nhap toi da 2 chu so sau dau thap phan.");

        return value.Value;
    }

    private static string NormalizeRequiredText(string value, string field, string label)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw AppException.BadRequest($"{label} la bat buoc.", field, "Khong duoc de trong.");
        return normalized;
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProductAuditSnapshot ProductSnapshot(Product entity, string? categoryCode, string? categoryName)
        => new(
            entity.Id,
            entity.ProductCode,
            entity.ProductName,
            entity.CategoryId,
            categoryCode,
            categoryName,
            entity.Unit,
            entity.Price,
            entity.Quantity,
            entity.Description,
            entity.IsActive);

    private static IReadOnlyList<string> GetChangedProductFields(ProductAuditSnapshot oldValue, ProductAuditSnapshot newValue)
    {
        var fields = new List<string>();
        if (oldValue.ProductName != newValue.ProductName) fields.Add(nameof(Product.ProductName));
        if (oldValue.Unit != newValue.Unit) fields.Add(nameof(Product.Unit));
        if (oldValue.Price != newValue.Price) fields.Add(nameof(Product.Price));
        if (oldValue.Quantity != newValue.Quantity) fields.Add(nameof(Product.Quantity));
        if (oldValue.Description != newValue.Description) fields.Add(nameof(Product.Description));
        if (oldValue.IsActive != newValue.IsActive) fields.Add(nameof(Product.IsActive));
        return fields;
    }

    private sealed record ProductAuditSnapshot(
        int Id,
        string ProductCode,
        string ProductName,
        int CategoryId,
        string? CategoryCode,
        string? CategoryName,
        string Unit,
        decimal Price,
        decimal Quantity,
        string? Description,
        bool IsActive);
}
