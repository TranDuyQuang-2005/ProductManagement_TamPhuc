using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.Categories;

namespace ProductManagement.Api.Repositories.Interfaces;

public interface ICategoryQueryRepository
{
    Task<PagedResult<CategoryResponse>> SearchAsync(CategorySearchRequest request, CancellationToken cancellationToken);
    Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductCodePreviewResponse?> GetProductCodePreviewAsync(int id, CancellationToken cancellationToken);
    Task<CategoryOptionResponse?> GetOptionByIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CategoryOptionResponse>> GetOptionsAsync(bool activeOnly, CancellationToken cancellationToken);
}
