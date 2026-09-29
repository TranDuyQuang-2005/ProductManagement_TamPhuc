using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.DTOs.Products;

namespace ProductManagement.Api.Services.Interfaces;

public interface IProductService
{
    Task<PagedResult<ProductResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken);
    Task<PagedResult<ProductResponse>> SearchTrashAsync(ProductSearchRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ProductResponse> CreateAsync(ProductCreateRequest request, CancellationToken cancellationToken);
    Task<ProductResponse> UpdateAsync(int id, ProductUpdateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
    Task<ProductResponse> RestoreAsync(int id, CancellationToken cancellationToken);
    Task DeletePermanentAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<AuditLogResponse>> GetHistoryAsync(int id, int page, int pageSize, CancellationToken cancellationToken);
}
