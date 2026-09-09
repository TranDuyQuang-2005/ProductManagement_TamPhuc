using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.Products;

namespace ProductManagement.Api.Repositories.Interfaces;

public interface IProductQueryRepository
{
    Task<PagedResult<ProductResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);
    Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
}
