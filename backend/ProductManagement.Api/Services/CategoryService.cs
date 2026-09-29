using System.Data;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.Categories;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Repositories.Interfaces;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Services;

public sealed partial class CategoryService(
    ICategoryQueryRepository queryRepository,
    ICategoryCommandRepository commandRepository,
    IProductCommandRepository productCommandRepository,
    IAuditService auditService,
    ICurrentUserService currentUser,
    AppDbContext dbContext) : ICategoryService
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "categoryCode", "categoryName", "productCount", "createdAt"
    };

    public async Task<PagedResult<CategoryResponse>> SearchAsync(CategorySearchRequest request, CancellationToken cancellationToken)
    {
        ValidateSearchRequest(request);
        return await queryRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken)
        => await queryRepository.GetByIdAsync(id, cancellationToken)
           ?? throw AppException.NotFound($"Không tìm thấy danh mục có Id = {id}.");

    public Task<IReadOnlyList<CategoryOptionResponse>> GetOptionsAsync(bool activeOnly, CancellationToken cancellationToken)
        => queryRepository.GetOptionsAsync(activeOnly, cancellationToken);

    public async Task<ProductCodePreviewResponse> GetProductCodePreviewAsync(int id, CancellationToken cancellationToken)
    {
        var preview = await queryRepository.GetProductCodePreviewAsync(id, cancellationToken)
            ?? throw AppException.NotFound($"Không tìm thấy danh mục có Id = {id}.");

        return preview;
    }

    public async Task<CategoryResponse> CreateAsync(CategoryCreateRequest request, CancellationToken cancellationToken)
    {
        var categoryCode = NormalizeCode(request.CategoryCode, "categoryCode", "Mã danh mục");
        var categoryName = NormalizeName(request.CategoryName);
        var codePrefix = NormalizePrefix(request.CodePrefix);
        var description = NormalizeOptionalText(request.Description);

        if (await commandRepository.ExistsByCodeAsync(categoryCode, null, cancellationToken))
            throw AppException.Conflict($"Mã danh mục '{categoryCode}' đã tồn tại.", "categoryCode", "Mã danh mục đã tồn tại.");

        if (await commandRepository.ExistsByPrefixAsync(codePrefix, null, cancellationToken))
            throw AppException.Conflict($"Ký hiệu danh mục \"{codePrefix}\" đã được sử dụng.", "codePrefix", "Ký hiệu danh mục đã được sử dụng.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var entity = new Category
        {
            CategoryCode = categoryCode,
            CategoryName = categoryName,
            CodePrefix = codePrefix,
            NextProductNumber = 1,
            Description = description,
            IsActive = request.IsActive,
            CreatedByUserId = currentUser.UserId,
            IsAdminProtected = RecordAccessPolicy.ShouldProtectAfterAction(currentUser.Role, false),
            CreatedAt = DateTime.UtcNow
        };

        await commandRepository.AddAsync(entity, cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await auditService.AddAsync("CREATE", "CATEGORY", entity.Id.ToString(), entity.CategoryCode, null,
            CategorySnapshot(entity, 0), null, $"Đã tạo danh mục {entity.CategoryCode}.", cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, CategoryUpdateRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var entity = await dbContext.Categories
            .FromSqlInterpolated($"SELECT * FROM dbo.Categories WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound($"Không tìm thấy danh mục có Id = {id}.");

        RecordAccessPolicy.EnsureCanModify(
            currentUser.Role, entity.IsAdminProtected, "Danh mục");

        var categoryCode = NormalizeCode(request.CategoryCode, "categoryCode", "Mã danh mục");
        var categoryName = NormalizeName(request.CategoryName);
        var codePrefix = NormalizePrefix(request.CodePrefix);
        var description = NormalizeOptionalText(request.Description);

        if (await commandRepository.ExistsByCodeAsync(categoryCode, id, cancellationToken))
            throw AppException.Conflict($"Mã danh mục '{categoryCode}' đã được sử dụng bởi danh mục khác.", "categoryCode", "Mã danh mục đã tồn tại.");

        if (await commandRepository.ExistsByPrefixAsync(codePrefix, id, cancellationToken))
            throw AppException.Conflict($"Ký hiệu danh mục \"{codePrefix}\" đã được sử dụng.", "codePrefix", "Ký hiệu danh mục đã được sử dụng.");

        var hasProducts = await productCommandRepository.AnyByCategoryIdAsync(id, cancellationToken);
        if (hasProducts && !string.Equals(entity.CodePrefix, codePrefix, StringComparison.Ordinal))
        {
            throw AppException.Conflict(
                "Không thể thay đổi ký hiệu danh mục vì danh mục đã có hàng hóa.",
                "codePrefix",
                "Ký hiệu không thể thay đổi vì danh mục đã có hàng hóa.");
        }

        var oldSnapshot = CategorySnapshot(entity, hasProducts ? 1 : 0);
        entity.CategoryCode = categoryCode;
        entity.CategoryName = categoryName;
        entity.CodePrefix = codePrefix;
        entity.Description = description;
        entity.IsActive = request.IsActive!.Value;
        entity.LastModifiedByUserId = currentUser.UserId;
        entity.IsAdminProtected = RecordAccessPolicy.ShouldProtectAfterAction(currentUser.Role, entity.IsAdminProtected);
        entity.UpdatedAt = DateTime.UtcNow;

        await commandRepository.SaveChangesAsync(cancellationToken);
        var newSnapshot = CategorySnapshot(entity, hasProducts ? 1 : 0);
        await auditService.AddAsync("UPDATE", "CATEGORY", entity.Id.ToString(), entity.CategoryCode, oldSnapshot,
            newSnapshot, GetChangedCategoryFields(oldSnapshot, newSnapshot), $"Đã cập nhật danh mục {entity.CategoryCode}.", cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var entity = await dbContext.Categories
            .FromSqlInterpolated($"SELECT * FROM dbo.Categories WITH (UPDLOCK, ROWLOCK) WHERE Id = {id}")
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw AppException.NotFound($"Không tìm thấy danh mục có Id = {id}.");

        RecordAccessPolicy.EnsureCanModify(
            currentUser.Role, entity.IsAdminProtected, "Danh mục");

        if (await productCommandRepository.AnyByCategoryIdAsync(id, cancellationToken))
        {
            throw AppException.Conflict("Không thể xóa danh mục vì đang có hàng hóa thuộc danh mục này.");
        }

        var oldSnapshot = CategorySnapshot(entity, 0);
        commandRepository.Remove(entity);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await auditService.AddAsync("DELETE", "CATEGORY", entity.Id.ToString(), entity.CategoryCode, oldSnapshot,
            null, null, $"Đã xóa danh mục {entity.CategoryCode}.", cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void ValidateSearchRequest(CategorySearchRequest request)
    {
        if (!AllowedSortFields.Contains(request.SortBy))
            throw AppException.BadRequest("Trường sắp xếp không hợp lệ.", "sortBy", "Vui lòng chọn trường sắp xếp hợp lệ.");

        if (!string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw AppException.BadRequest("Chiều sắp xếp không hợp lệ.", "sortDirection", "Vui lòng chọn chiều sắp xếp hợp lệ.");
    }

    private static string NormalizeCode(string value, string field, string label)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw AppException.BadRequest($"{label} là bắt buộc.", field, "Không được để trống.");
        return normalized;
    }

    private static string NormalizeName(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw AppException.BadRequest("Tên danh mục là bắt buộc.", "categoryName", "Không được để trống.");
        return normalized;
    }

    private static string NormalizePrefix(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (!CodePrefixRegex().IsMatch(normalized))
        {
            throw AppException.BadRequest(
                "Ký hiệu danh mục chỉ gồm chữ cái/số và dài từ 2 đến 10 ký tự.",
                "codePrefix",
                "Chỉ chấp nhận chữ cái/số, từ 2 đến 10 ký tự.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static CategoryAuditSnapshot CategorySnapshot(Category entity, int productCount)
        => new(entity.Id, entity.CategoryCode, entity.CategoryName, entity.CodePrefix,
            entity.NextProductNumber, entity.Description, entity.IsActive, productCount);

    private static IReadOnlyList<string> GetChangedCategoryFields(CategoryAuditSnapshot oldValue, CategoryAuditSnapshot newValue)
    {
        var fields = new List<string>();
        if (oldValue.CategoryCode != newValue.CategoryCode) fields.Add(nameof(Category.CategoryCode));
        if (oldValue.CategoryName != newValue.CategoryName) fields.Add(nameof(Category.CategoryName));
        if (oldValue.CodePrefix != newValue.CodePrefix) fields.Add(nameof(Category.CodePrefix));
        if (oldValue.Description != newValue.Description) fields.Add(nameof(Category.Description));
        if (oldValue.IsActive != newValue.IsActive) fields.Add(nameof(Category.IsActive));
        return fields;
    }

    [GeneratedRegex("^[A-Z0-9]{2,10}$")]
    private static partial Regex CodePrefixRegex();

    private sealed record CategoryAuditSnapshot(
        int Id,
        string CategoryCode,
        string CategoryName,
        string CodePrefix,
        int NextProductNumber,
        string? Description,
        bool IsActive,
        int ProductCount);
}
