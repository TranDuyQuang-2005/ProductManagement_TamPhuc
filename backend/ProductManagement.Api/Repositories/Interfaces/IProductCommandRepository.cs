using ProductManagement.Api.Entities;

namespace ProductManagement.Api.Repositories.Interfaces;

public interface IProductCommandRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsByCodeAsync(string productCode, int? excludeId, CancellationToken cancellationToken);
    Task<bool> AnyByCategoryIdAsync(int categoryId, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    void Remove(Product product);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
