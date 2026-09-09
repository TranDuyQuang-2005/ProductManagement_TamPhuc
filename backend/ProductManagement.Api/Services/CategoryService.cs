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
           ?? throw AppException.NotFound($"Khong tim thay danh muc co Id = {id}.");

    public Task<IReadOnlyList<CategoryOptionResponse>> GetOptionsAsync(bool activeOnly, CancellationToken cancellationToken)
        => queryRepository.GetOptionsAsync(activeOnly, cancellationToken);

    public async Task<ProductCodePreviewResponse> GetProductCodePreviewAsync(int id, CancellationToken cancellationToken)
    {
        var preview = await queryRepository.GetProductCodePreviewAsync(id, cancellationToken)
            ?? throw AppException.NotFound($"Khong tim thay danh muc co Id = {id}.");

        return preview;
    }

    public async Task<CategoryResponse> CreateAsync(CategoryCreateRequest request, CancellationToken cancellationToken)
    {
        var categoryCode = NormalizeCode(request.CategoryCode, "categoryCode", "Ma danh muc");
        var categoryName = NormalizeName(request.CategoryName);
        var codePrefix = NormalizePrefix(request.CodePrefix);
        var description = NormalizeOptionalText(request.Description);

        if (await commandRepository.ExistsByCodeAsync(categoryCode, null, cancellationToken))
            throw AppException.Conflict($"Ma danh muc '{categoryCode}' da ton tai.", "categoryCode", "Ma danh muc da ton tai.");

        if (await commandRepository.ExistsByPrefixAsync(codePrefix, null, cancellationToken))
            throw AppException.Conflict($"Ky hieu danh muc \"{codePrefix}\" da duoc su dung.", "codePrefix", "Ky hieu danh muc da duoc su dung.");

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
            CreatedByUsername = currentUser.Username,
            CreatedByRole = NormalizeRole(currentUser.Role),
            CreatedAt = DateTime.UtcNow
        };

        await commandRepository.AddAsync(entity, cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await auditService.AddAsync("CREATE", "CATEGORY", entity.Id.ToString(), entity.CategoryCode, null,
            CategorySnapshot(entity, 0), null, $"Created category {entity.CategoryCode}.", cancellationToken);
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
            ?? throw AppException.NotFound($"Khong tim thay danh muc co Id = {id}.");

        RecordAccessPolicy.EnsureCanModify(
            currentUser.Role, entity.CreatedByRole, entity.LastModifiedByRole, "Danh muc");

        var categoryCode = NormalizeCode(request.CategoryCode, "categoryCode", "Ma danh muc");
        var categoryName = NormalizeName(request.CategoryName);
        var codePrefix = NormalizePrefix(request.CodePrefix);
        var description = NormalizeOptionalText(request.Description);

        if (await commandRepository.ExistsByCodeAsync(categoryCode, id, cancellationToken))
            throw AppException.Conflict($"Ma danh muc '{categoryCode}' da duoc su dung boi danh muc khac.", "categoryCode", "Ma danh muc da ton tai.");

        if (await commandRepository.ExistsByPrefixAsync(codePrefix, id, cancellationToken))
            throw AppException.Conflict($"Ky hieu danh muc \"{codePrefix}\" da duoc su dung.", "codePrefix", "Ky hieu danh muc da duoc su dung.");

        var hasProducts = await productCommandRepository.AnyByCategoryIdAsync(id, cancellationToken);
        if (hasProducts && !string.Equals(entity.CodePrefix, codePrefix, StringComparison.Ordinal))
        {
            throw AppException.Conflict(
                "Khong the thay doi ky hieu danh muc vi danh muc da co hang hoa.",
                "codePrefix",
                "Ky hieu khong the thay doi vi danh muc da co hang hoa.");
        }

        var oldSnapshot = CategorySnapshot(entity, hasProducts ? 1 : 0);
        entity.CategoryCode = categoryCode;
        entity.CategoryName = categoryName;
        entity.CodePrefix = codePrefix;
        entity.Description = description;
        entity.IsActive = request.IsActive!.Value;
        entity.LastModifiedByUserId = currentUser.UserId;
        entity.LastModifiedByUsername = currentUser.Username;
        entity.LastModifiedByRole = NormalizeRole(currentUser.Role);
        entity.UpdatedAt = DateTime.UtcNow;

        await commandRepository.SaveChangesAsync(cancellationToken);
        var newSnapshot = CategorySnapshot(entity, hasProducts ? 1 : 0);
        await auditService.AddAsync("UPDATE", "CATEGORY", entity.Id.ToString(), entity.CategoryCode, oldSnapshot,
            newSnapshot, GetChangedCategoryFields(oldSnapshot, newSnapshot), $"Updated category {entity.CategoryCode}.", cancellationToken);
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
            ?? throw AppException.NotFound($"Khong tim thay danh muc co Id = {id}.");

        RecordAccessPolicy.EnsureCanModify(
            currentUser.Role, entity.CreatedByRole, entity.LastModifiedByRole, "Danh muc");

        if (await productCommandRepository.AnyByCategoryIdAsync(id, cancellationToken))
        {
            throw AppException.Conflict("Khong the xoa danh muc vi dang co hang hoa thuoc danh muc nay.");
        }

        var oldSnapshot = CategorySnapshot(entity, 0);
        commandRepository.Remove(entity);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await auditService.AddAsync("DELETE", "CATEGORY", entity.Id.ToString(), entity.CategoryCode, oldSnapshot,
            null, null, $"Deleted category {entity.CategoryCode}.", cancellationToken);
        await commandRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void ValidateSearchRequest(CategorySearchRequest request)
    {
        if (!AllowedSortFields.Contains(request.SortBy))
            throw AppException.BadRequest("Truong sap xep khong hop le.", "sortBy", $"Chi ho tro: {string.Join(", ", AllowedSortFields)}.");

        if (!string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
            throw AppException.BadRequest("Chieu sap xep khong hop le.", "sortDirection", "Chi chap nhan 'asc' hoac 'desc'.");
    }

    private static string NormalizeCode(string value, string field, string label)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            throw AppException.BadRequest($"{label} la bat buoc.", field, "Khong duoc de trong.");
        return normalized;
    }

    private static string NormalizeName(string value)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            throw AppException.BadRequest("Ten danh muc la bat buoc.", "categoryName", "Khong duoc de trong.");
        return normalized;
    }

    private static string NormalizePrefix(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        if (!CodePrefixRegex().IsMatch(normalized))
        {
            throw AppException.BadRequest(
                "Ky hieu danh muc chi gom chu cai/so va dai tu 2 den 10 ky tu.",
                "codePrefix",
                "Chi chap nhan chu cai/so, tu 2 den 10 ky tu.");
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

    private static string NormalizeRole(string? role)
        => string.IsNullOrWhiteSpace(role) ? string.Empty : role.Trim().ToUpperInvariant();

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
