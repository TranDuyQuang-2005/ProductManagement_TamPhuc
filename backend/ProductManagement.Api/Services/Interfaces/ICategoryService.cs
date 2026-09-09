using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.Categories;

namespace ProductManagement.Api.Services.Interfaces;

public interface ICategoryService
{
    Task<PagedResult<CategoryResponse>> SearchAsync(CategorySearchRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductCodePreviewResponse> GetProductCodePreviewAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryOptionResponse>> GetOptionsAsync(bool activeOnly, CancellationToken cancellationToken);
    Task<CategoryResponse> CreateAsync(CategoryCreateRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse> UpdateAsync(int id, CategoryUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
