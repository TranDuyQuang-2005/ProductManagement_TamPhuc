using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Data;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Repositories.Interfaces;

namespace ProductManagement.Api.Repositories;

public sealed class ProductCommandRepository(AppDbContext dbContext) : IProductCommandRepository
{
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
        => dbContext.Products.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> ExistsByCodeAsync(string productCode, int? excludeId, CancellationToken cancellationToken)
        => dbContext.Products.AnyAsync(
            x => x.ProductCode == productCode && (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> AnyByCategoryIdAsync(int categoryId, CancellationToken cancellationToken)
        => dbContext.Products.AnyAsync(x => x.CategoryId == categoryId, cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
        => await dbContext.Products.AddAsync(product, cancellationToken);

    public void Remove(Product product) => dbContext.Products.Remove(product);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => dbContext.SaveChangesAsync(cancellationToken);
}
