using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Data;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Repositories.Interfaces;

namespace ProductManagement.Api.Repositories;

public sealed class CategoryCommandRepository(AppDbContext dbContext) : ICategoryCommandRepository
{
    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string categoryCode, int? excludeId, CancellationToken cancellationToken)
        => dbContext.Categories.AnyAsync(
            x => x.CategoryCode == categoryCode && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> ExistsByPrefixAsync(string codePrefix, int? excludeId, CancellationToken cancellationToken)
        => dbContext.Categories.AnyAsync(
            x => x.CodePrefix == codePrefix && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
        => await dbContext.Categories.AddAsync(category, cancellationToken);

    public void Remove(Category category) => dbContext.Categories.Remove(category);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
