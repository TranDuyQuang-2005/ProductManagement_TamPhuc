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
        "productCode", "productName", "categoryName", "price", "stockQuantity", "createdAt"
    };

    public async Task<PagedResult<ProductResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        ValidateSearchRequest(request);
        return await queryRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<PagedResult<ProductResponse>> SearchTrashAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        EnsureAdmin("Ban khong co quyen xem danh sach hang hoa da xoa.");
        ValidateSearchRequest(request);
        return await queryRepository.SearchTrashAsync(request, cancellationToken);
    }

    public async Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
        => await queryRepository.GetByIdAsync(id, cancellationToken)
           ?? throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

    public async Task<ProductResponse> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken)
    {
        var productName = NormalizeRequiredProductName(request.ProductName);
        var unit = NormalizeRequiredText(request.Unit, "unit", "Don vi tinh");
        var description = NormalizeOptionalText(request.Description);
        var price = ValidateAmount(request.Price, "price", "Gia ban");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var category = await dbContext.Categories
            .FromSqlInterpolated($"SELECT * FROM dbo.Categories WITH (UPDLOCK, ROWLOCK) WHERE Id = {request.CategoryId}")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.BadRequest("Danh muc duoc chon khong ton tai.", "categoryId", "Danh muc khong ton tai.");

        if (!category.IsActive)
            throw AppException.BadRequest("Khong the gan hang hoa vao danh muc dang ngung hoat dong.", "categoryId", "Danh muc dang ngung hoat dong.");

        var productCode = $"{category.CodePrefix}{category.NextProductNumber:000000}";
        category.NextProductNumber++;

        var entity = new Product
        {
            ProductCode = productCode,
            ProductName = productName,
            CategoryId = category.Id,
            Unit = unit,
            Price = price,
            StockQuantity = 0m,
            Description = description,
            IsActive = request.IsActive,
            CreatedByUserId = currentUser.UserId,
            IsAdminProtected = RecordAccessPolicy.ShouldProtectAfterAction(currentUser.Role, false),
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
            $"Da tao hang hoa {entity.ProductCode}.",
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

        EnsureCanModifyProduct(entity, "Ban khong co quyen chinh sua hang hoa nay.");

        var productName = NormalizeRequiredProductName(request.ProductName);
        var unit = NormalizeRequiredText(request.Unit, "unit", "Don vi tinh");
        var description = NormalizeOptionalText(request.Description);
        var price = ValidateAmount(request.Price, "price", "Gia ban");
        var category = await categoryQueryRepository.GetOptionByIdAsync(entity.CategoryId, cancellationToken);
        var oldSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);

        entity.ProductName = productName;
        entity.Unit = unit;
        entity.Price = price;
        entity.Description = description;
        entity.IsActive = request.IsActive!.Value;
        entity.LastModifiedByUserId = currentUser.UserId;
        entity.IsAdminProtected = RecordAccessPolicy.ShouldProtectAfterAction(currentUser.Role, entity.IsAdminProtected);
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
            $"Da cap nhat hang hoa {entity.ProductCode}.",
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

        EnsureCanModifyProduct(entity, "Ban khong co quyen xoa hang hoa nay.");

        if (entity.StockQuantity > 0)
            throw AppException.Conflict("Khong the xoa hang hoa khi van con ton kho. Vui long xu ly ton kho truoc.");

        var category = await categoryQueryRepository.GetOptionByIdAsync(entity.CategoryId, cancellationToken);
        var oldSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.DeletedByUserId = currentUser.UserId;
        entity.UpdatedAt = now;

        await commandRepository.SaveChangesAsync(cancellationToken);
        await auditService.AddAsync(
            "DELETE",
            "PRODUCT",
            entity.Id.ToString(),
            entity.ProductCode,
            oldSnapshot,
            null,
            null,
            $"Da xoa hang hoa {entity.ProductCode}.",
            cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<ProductResponse> RestoreAsync(int id, CancellationToken cancellationToken)
    {
        EnsureAdmin("Ban khong co quyen khoi phuc hang hoa nay.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var entity = await dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

        if (!entity.IsDeleted)
            throw AppException.Conflict("Hang hoa nay chua bi xoa nen khong can khoi phuc.");

        var category = await categoryQueryRepository.GetOptionByIdAsync(entity.CategoryId, cancellationToken);
        var oldSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedByUserId = null;
        entity.UpdatedAt = DateTime.UtcNow;

        await commandRepository.SaveChangesAsync(cancellationToken);
        var newSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);
        await auditService.AddAsync(
            "RESTORE",
            "PRODUCT",
            entity.Id.ToString(),
            entity.ProductCode,
            oldSnapshot,
            newSnapshot,
            null,
            $"Da khoi phuc hang hoa {entity.ProductCode}.",
            cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeletePermanentAsync(int id, CancellationToken cancellationToken)
    {
        EnsureAdmin("Ban khong co quyen xoa vinh vien hang hoa nay.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var entity = await dbContext.Products
            .FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

        if (!entity.IsDeleted)
            throw AppException.Conflict("Chi co the xoa vinh vien hang hoa da nam trong danh sach da xoa.");

        var category = await categoryQueryRepository.GetOptionByIdAsync(entity.CategoryId, cancellationToken);
        var oldSnapshot = ProductSnapshot(entity, category?.CategoryCode, category?.CategoryName);
        await auditService.AddAsync(
            "PERMANENT_DELETE",
            "PRODUCT",
            entity.Id.ToString(),
            entity.ProductCode,
            oldSnapshot,
            null,
            null,
            $"Da xoa vinh vien hang hoa {entity.ProductCode}.",
            cancellationToken);
        commandRepository.Remove(entity);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<PagedResult<AuditLogResponse>> GetHistoryAsync(int id, int page, int pageSize, CancellationToken cancellationToken)
    {
        var product = await queryRepository.GetByIdAsync(id, cancellationToken);
        var exists = product is not null
                     || await dbContext.Products.IgnoreQueryFilters().AnyAsync(x => x.Id == id, cancellationToken);
        if (!exists)
            throw AppException.NotFound($"Khong tim thay hang hoa co Id = {id}.");

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

    private void EnsureAdmin(string message)
    {
        if (!string.Equals(currentUser.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            throw AppException.Forbidden(message);
    }

    private void EnsureCanModifyProduct(Product entity, string message)
    {
        if (!RecordAccessPolicy.CanModify(currentUser.Role, entity.IsAdminProtected))
            throw AppException.Forbidden(message);
    }

    private static void ValidateSearchRequest(ProductSearchRequest request)
    {
        if (!Enum.IsDefined(request.StockStatus))
            throw AppException.BadRequest("Trang thai ton kho khong hop le.", "stockStatus", "Vui long chon trang thai ton kho hop le.");

        if (request.MinPrice.HasValue && request.MaxPrice.HasValue && request.MinPrice > request.MaxPrice)
            throw AppException.BadRequest("Gia tu khong duoc lon hon gia den.", "minPrice", "Gia tu phai nho hon hoac bang gia den.");

        if (!AllowedSortFields.Contains(request.SortBy))
            throw AppException.BadRequest("Truong sap xep khong hop le.", "sortBy", "Vui long chon truong sap xep hop le.");

        if (!string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw AppException.BadRequest("Chieu sap xep khong hop le.", "sortDirection", "Vui long chon chieu sap xep hop le.");
    }

    internal static decimal ValidateAmount(decimal? value, string field, string label)
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

    private static string NormalizeRequiredProductName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw AppException.BadRequest("Ten hang hoa la bat buoc.", "productName", "Khong duoc de trong.");

        var normalized = TextNormalization.NormalizeNfc(value);
        if (string.IsNullOrWhiteSpace(normalized))
            throw AppException.BadRequest("Ten hang hoa la bat buoc.", "productName", "Khong duoc de trong.");
        return normalized;
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    internal static ProductAuditSnapshot ProductSnapshot(Product entity, string? categoryCode, string? categoryName)
        => new(
            entity.Id,
            entity.ProductCode,
            entity.ProductName,
            entity.CategoryId,
            categoryCode,
            categoryName,
            entity.Unit,
            entity.Price,
            entity.StockQuantity,
            entity.Description,
            entity.IsActive);

    private static IReadOnlyList<string> GetChangedProductFields(ProductAuditSnapshot oldValue, ProductAuditSnapshot newValue)
    {
        var fields = new List<string>();
        if (oldValue.ProductName != newValue.ProductName) fields.Add(nameof(Product.ProductName));
        if (oldValue.Unit != newValue.Unit) fields.Add(nameof(Product.Unit));
        if (oldValue.Price != newValue.Price) fields.Add(nameof(Product.Price));
        if (oldValue.StockQuantity != newValue.StockQuantity) fields.Add(nameof(Product.StockQuantity));
        if (oldValue.Description != newValue.Description) fields.Add(nameof(Product.Description));
        if (oldValue.IsActive != newValue.IsActive) fields.Add(nameof(Product.IsActive));
        return fields;
    }

    internal sealed record ProductAuditSnapshot(
        int Id,
        string ProductCode,
        string ProductName,
        int CategoryId,
        string? CategoryCode,
        string? CategoryName,
        string Unit,
        decimal Price,
        decimal StockQuantity,
        string? Description,
        bool IsActive);
}
