using ProductManagement.Api.Entities;

namespace ProductManagement.Api.Repositories.Interfaces;

public interface ICategoryCommandRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsByCodeAsync(string categoryCode, int? excludeId, CancellationToken cancellationToken);
    Task<bool> ExistsByPrefixAsync(string codePrefix, int? excludeId, CancellationToken cancellationToken);
    Task AddAsync(Category category, CancellationToken cancellationToken);
    void Remove(Category category);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
