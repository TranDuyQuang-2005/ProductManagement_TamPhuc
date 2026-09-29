using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.DTOs.Inventory;
using ProductManagement.Api.DTOs.Products;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Services.Interfaces;
using ProductManagement.Api.Tests.Support;

namespace ProductManagement.Api.Tests;

public sealed class ProductServiceIntegrationTests : IClassFixture<SqlServerTestDatabaseFixture>, IAsyncLifetime
{
    private readonly SqlServerTestDatabaseFixture _database;
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;

    public ProductServiceIntegrationTests(SqlServerTestDatabaseFixture database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        await _database.ResetAsync();
        _provider = TestServiceProviderFactory.Create(_database.ConnectionString);
        _scope = _provider.CreateScope();
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        if (_provider is not null)
            await _provider.DisposeAsync();
    }

    [Theory]
    [InlineData("   ", "kg", "1", "productName")]
    [InlineData("Valid", "   ", "1", "unit")]
    [InlineData("Valid", "kg", null, "price")]
    [InlineData("Valid", "kg", "-0.01", "price")]
    [InlineData("Valid", "kg", "1.001", "price")]
    public async Task Create_rejects_required_amount_and_precision_rules(
        string? productName,
        string unit,
        string? price,
        string expectedField)
    {
        var categoryId = await SeedCategoryAsync("VAL", 1);
        var service = Get<IProductService>();

        var exception = await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(new ProductCreateRequest
        {
            ProductName = productName!,
            CategoryId = categoryId,
            Unit = unit,
            Price = price is null ? null : decimal.Parse(price, CultureInfo.InvariantCulture)
        }, CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
        Assert.True(exception.Errors?.ContainsKey(expectedField));
    }

    [Fact]
    public async Task Create_generates_sequential_product_codes_from_category_prefix()
    {
        var categoryId = await SeedCategoryAsync("PC", nextProductNumber: 42);
        var service = Get<IProductService>();

        var first = await service.CreateAsync(CreateRequest(categoryId, "First product", price: 12.34m), CancellationToken.None);
        var second = await service.CreateAsync(CreateRequest(categoryId, "Second product", price: 56.78m), CancellationToken.None);

        Assert.Equal("PC000042", first.ProductCode);
        Assert.Equal("PC000043", second.ProductCode);

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var category = await db.Categories.SingleAsync(x => x.Id == categoryId);
        Assert.Equal(44, category.NextProductNumber);
    }

    [Fact]
    public async Task Create_sets_stock_quantity_to_zero()
    {
        var categoryId = await SeedCategoryAsync("ZQ", 1);
        var service = Get<IProductService>();

        var created = await service.CreateAsync(CreateRequest(categoryId, "Zero stock product"), CancellationToken.None);

        Assert.Equal(0m, created.StockQuantity);
    }

    [Fact]
    public async Task Update_changes_editable_fields_but_keeps_product_code_and_category()
    {
        var categoryId = await SeedCategoryAsync("UP", 1);
        var service = Get<IProductService>();
        var created = await service.CreateAsync(CreateRequest(categoryId, "Old name", price: 10m), CancellationToken.None);

        var updated = await service.UpdateAsync(created.Id, new ProductUpdateRequest
        {
            ProductName = "New name",
            Unit = "box",
            Price = 20.25m,
            Description = "Updated description",
            IsActive = false
        }, CancellationToken.None);

        Assert.Equal(created.ProductCode, updated.ProductCode);
        Assert.Equal(categoryId, updated.CategoryId);
        Assert.Equal("New name", updated.ProductName);
        Assert.Equal("box", updated.Unit);
        Assert.Equal(20.25m, updated.Price);
        Assert.Equal(0m, updated.StockQuantity);
        Assert.False(updated.IsActive);
        Assert.NotNull(updated.LastModifiedByUserId);
    }

    [Fact]
    public async Task Update_does_not_change_stock_quantity()
    {
        var categoryId = await SeedCategoryAsync("SK", 1);
        var productService = Get<IProductService>();
        var inventoryService = Get<IInventoryService>();
        var created = await productService.CreateAsync(CreateRequest(categoryId, "Stock-safe update"), CancellationToken.None);
        await inventoryService.StockInAsync(new StockInRequest { ProductId = created.Id, Quantity = 7.5m }, CancellationToken.None);

        var updated = await productService.UpdateAsync(created.Id, new ProductUpdateRequest
        {
            ProductName = "Stock-safe update changed",
            Unit = "box",
            Price = 22m,
            Description = "Metadata only",
            IsActive = true
        }, CancellationToken.None);

        Assert.Equal(7.5m, updated.StockQuantity);
    }

    [Fact]
    public async Task StockIn_accumulates_stock_creates_inventory_transaction_and_audit()
    {
        var categoryId = await SeedCategoryAsync("IN", 1);
        var productService = Get<IProductService>();
        var inventoryService = Get<IInventoryService>();
        var created = await productService.CreateAsync(CreateRequest(categoryId, "Stock in product"), CancellationToken.None);

        var first = await inventoryService.StockInAsync(new StockInRequest
        {
            ProductId = created.Id,
            Quantity = 10m,
            ReferenceCode = "pn001",
            Note = "first"
        }, CancellationToken.None);
        var second = await inventoryService.StockInAsync(new StockInRequest
        {
            ProductId = created.Id,
            Quantity = 2.5m,
            ReferenceCode = "pn002",
            Note = "second"
        }, CancellationToken.None);

        var product = await productService.GetByIdAsync(created.Id, CancellationToken.None);
        Assert.Equal(10m, first.QuantityAfter);
        Assert.Equal(10m, second.QuantityBefore);
        Assert.Equal(12.5m, second.QuantityAfter);
        Assert.Equal(12.5m, product.StockQuantity);

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var transactions = await db.InventoryTransactions
            .Where(x => x.ProductId == created.Id)
            .OrderBy(x => x.Id)
            .ToListAsync();
        Assert.Equal(2, transactions.Count);
        Assert.Equal(0m, transactions[0].QuantityBefore);
        Assert.Equal(10m, transactions[0].QuantityChange);
        Assert.Equal(10m, transactions[0].QuantityAfter);
        Assert.Equal(2.5m, transactions[1].QuantityChange);
        Assert.Equal("test-admin-id", transactions[1].CreatedByUserId);

        var audit = await Get<IAuditLogService>().SearchAsync(new AuditLogSearchRequest
        {
            EntityType = "PRODUCT",
            EntityId = created.Id.ToString(),
            Action = "STOCK_IN",
            PageSize = 20
        }, CancellationToken.None);
        Assert.Equal(2, audit.TotalItems);
        Assert.All(audit.Items, x => Assert.Equal("STOCK_IN", x.Action));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1.001")]
    public async Task StockIn_rejects_invalid_quantities(string quantityText)
    {
        var categoryId = await SeedCategoryAsync("VI", 1);
        var product = await Get<IProductService>().CreateAsync(CreateRequest(categoryId, "Invalid stock in"), CancellationToken.None);
        var quantity = decimal.Parse(quantityText, CultureInfo.InvariantCulture);

        var exception = await Assert.ThrowsAsync<AppException>(() => Get<IInventoryService>().StockInAsync(new StockInRequest
        {
            ProductId = product.Id,
            Quantity = quantity
        }, CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task StockIn_rejects_missing_or_deleted_product()
    {
        var categoryId = await SeedCategoryAsync("MD", 1);
        var productService = Get<IProductService>();
        var inventoryService = Get<IInventoryService>();
        var product = await productService.CreateAsync(CreateRequest(categoryId, "Deleted stock target"), CancellationToken.None);

        var missing = await Assert.ThrowsAsync<AppException>(() => inventoryService.StockInAsync(new StockInRequest
        {
            ProductId = 999_999,
            Quantity = 1m
        }, CancellationToken.None));
        Assert.Equal(404, missing.StatusCode);

        await productService.DeleteAsync(product.Id, CancellationToken.None);
        var deleted = await Assert.ThrowsAsync<AppException>(() => inventoryService.StockInAsync(new StockInRequest
        {
            ProductId = product.Id,
            Quantity = 1m
        }, CancellationToken.None));
        Assert.Equal(404, deleted.StatusCode);
    }

    [Fact]
    public async Task Delete_conflicts_when_stock_positive_and_succeeds_when_zero()
    {
        var categoryId = await SeedCategoryAsync("DX", 1);
        var productService = Get<IProductService>();
        var inventoryService = Get<IInventoryService>();
        var stocked = await productService.CreateAsync(CreateRequest(categoryId, "Stocked delete"), CancellationToken.None);
        var empty = await productService.CreateAsync(CreateRequest(categoryId, "Empty delete"), CancellationToken.None);
        await inventoryService.StockInAsync(new StockInRequest { ProductId = stocked.Id, Quantity = 1m }, CancellationToken.None);

        var conflict = await Assert.ThrowsAsync<AppException>(() => productService.DeleteAsync(stocked.Id, CancellationToken.None));
        Assert.Equal(409, conflict.StatusCode);

        await productService.DeleteAsync(empty.Id, CancellationToken.None);
        var deleted = await dbProductAsync(empty.Id);
        Assert.True(deleted?.IsDeleted);

        async Task<Product?> dbProductAsync(int id)
        {
            await using var scope = _provider.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await db.Products.IgnoreQueryFilters().SingleOrDefaultAsync(x => x.Id == id);
        }
    }

    [Fact]
    public async Task StockIn_concurrent_requests_do_not_lose_updates()
    {
        var categoryId = await SeedCategoryAsync("CN", 1);
        var product = await Get<IProductService>().CreateAsync(CreateRequest(categoryId, "Concurrent stock in"), CancellationToken.None);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => StockInInNewScopeAsync(product.Id, 1m)));

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var stored = await db.Products.SingleAsync(x => x.Id == product.Id);
        var transactionCount = await db.InventoryTransactions.CountAsync(x => x.ProductId == product.Id && x.MovementType == "STOCK_IN");
        Assert.Equal(20m, stored.StockQuantity);
        Assert.Equal(20, transactionCount);
    }

    private async Task StockInInNewScopeAsync(int productId, decimal quantity)
    {
        await using var scope = _provider.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        await service.StockInAsync(new StockInRequest { ProductId = productId, Quantity = quantity }, CancellationToken.None);
    }

    [Fact]
    public async Task Create_normalizes_product_name_to_unicode_nfc_before_saving()
    {
        var categoryId = await SeedCategoryAsync("UN", 1);
        var service = Get<IProductService>();
        var expectedName = "Th\u1ecbt b\u00f2";
        var nfdName = expectedName.Normalize(NormalizationForm.FormD);

        var created = await service.CreateAsync(CreateRequest(categoryId, nfdName), CancellationToken.None);
        var storedName = await ReadStoredProductNameAsync(created.Id);

        Assert.True(storedName.IsNormalized(NormalizationForm.FormC));
        Assert.Equal(expectedName, storedName);
        Assert.Equal(expectedName, created.ProductName);
    }

    [Fact]
    public async Task Update_normalizes_product_name_to_unicode_nfc_before_saving()
    {
        var categoryId = await SeedCategoryAsync("UU", 1);
        var service = Get<IProductService>();
        var created = await service.CreateAsync(CreateRequest(categoryId, "Before Unicode update"), CancellationToken.None);
        var expectedName = "Th\u1ecbt b\u00f2";
        var nfdName = expectedName.Normalize(NormalizationForm.FormD);

        var updated = await service.UpdateAsync(created.Id, new ProductUpdateRequest
        {
            ProductName = nfdName,
            Unit = "kg",
            Price = 20m,
            Description = "Unicode update",
            IsActive = true
        }, CancellationToken.None);
        var storedName = await ReadStoredProductNameAsync(created.Id);

        Assert.True(storedName.IsNormalized(NormalizationForm.FormC));
        Assert.Equal(expectedName, storedName);
        Assert.Equal(expectedName, updated.ProductName);
    }

    [Fact]
    public async Task Search_supports_exact_code_prefix_name_and_price_filter()
    {
        var categoryId = await SeedCategoryAsync("SR", 1);
        var otherCategoryId = await SeedCategoryAsync("OT", 1);
        var service = Get<IProductService>();

        var apple = await service.CreateAsync(CreateRequest(categoryId, "Apple Fuji", price: 10m), CancellationToken.None);
        var apricot = await service.CreateAsync(CreateRequest(categoryId, "Apricot", price: 25m), CancellationToken.None);
        await service.CreateAsync(CreateRequest(otherCategoryId, "Orange", price: 40m), CancellationToken.None);

        var exactCode = await service.SearchAsync(Search(keyword: apple.ProductCode), CancellationToken.None);
        var codePrefix = await service.SearchAsync(Search(keyword: "SR000"), CancellationToken.None);
        var productName = await service.SearchAsync(Search(keyword: "Apple"), CancellationToken.None);
        var priceFilter = await service.SearchAsync(Search(minPrice: 20m, maxPrice: 30m), CancellationToken.None);

        Assert.Single(exactCode.Items);
        Assert.Equal(apple.Id, exactCode.Items[0].Id);
        Assert.Contains(codePrefix.Items, x => x.Id == apple.Id);
        Assert.Contains(codePrefix.Items, x => x.Id == apricot.Id);
        Assert.Single(productName.Items);
        Assert.Equal(apple.Id, productName.Items[0].Id);
        Assert.Single(priceFilter.Items);
        Assert.Equal(apricot.Id, priceFilter.Items[0].Id);
    }

    [Fact]
    public async Task Search_matches_product_created_from_nfd_name_when_keyword_is_nfc()
    {
        var categoryId = await SeedCategoryAsync("US", 1);
        var service = Get<IProductService>();
        var productName = "Th\u1ecbt b\u00f2 cao c\u1ea5p";
        var created = await service.CreateAsync(CreateRequest(categoryId, productName.Normalize(NormalizationForm.FormD)), CancellationToken.None);

        var result = await service.SearchAsync(Search(keyword: "th\u1ecbt"), CancellationToken.None);

        Assert.Contains(result.Items, x => x.Id == created.Id);
    }

    [Fact]
    public async Task Search_matches_nfc_product_name_when_keyword_is_nfd()
    {
        var categoryId = await SeedCategoryAsync("UK", 1);
        var service = Get<IProductService>();
        var productName = "Th\u1ecbt b\u00f2 cao c\u1ea5p";
        var created = await service.CreateAsync(CreateRequest(categoryId, productName), CancellationToken.None);
        var nfdKeyword = "th\u1ecbt".Normalize(NormalizationForm.FormD);

        var result = await service.SearchAsync(Search(keyword: nfdKeyword), CancellationToken.None);

        Assert.Contains(result.Items, x => x.Id == created.Id);
    }

    [Fact]
    public async Task Search_rejects_min_price_greater_than_max_price()
    {
        var service = Get<IProductService>();

        var exception = await Assert.ThrowsAsync<AppException>(() => service.SearchAsync(
            Search(minPrice: 100m, maxPrice: 10m),
            CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
        Assert.True(exception.Errors?.ContainsKey("minPrice"));
    }

    [Fact]
    public async Task Soft_delete_hides_product_from_list_and_detail_and_shows_in_trash()
    {
        var categoryId = await SeedCategoryAsync("DL", 1);
        var service = Get<IProductService>();
        var created = await service.CreateAsync(CreateRequest(categoryId, "Deleted product"), CancellationToken.None);

        await service.DeleteAsync(created.Id, CancellationToken.None);

        var list = await service.SearchAsync(Search(keyword: created.ProductCode), CancellationToken.None);
        var trash = await service.SearchTrashAsync(Search(keyword: created.ProductCode), CancellationToken.None);
        var detailException = await Assert.ThrowsAsync<AppException>(() => service.GetByIdAsync(created.Id, CancellationToken.None));

        Assert.Empty(list.Items);
        Assert.Single(trash.Items);
        Assert.Equal(created.Id, trash.Items[0].Id);
        Assert.True(trash.Items[0].IsDeleted);
        Assert.Equal(404, detailException.StatusCode);
    }

    [Fact]
    public async Task Restore_moves_product_from_trash_back_to_active_list_and_detail()
    {
        var categoryId = await SeedCategoryAsync("RS", 1);
        var service = Get<IProductService>();
        var created = await service.CreateAsync(CreateRequest(categoryId, "Restored product"), CancellationToken.None);
        await service.DeleteAsync(created.Id, CancellationToken.None);

        var restored = await service.RestoreAsync(created.Id, CancellationToken.None);
        var list = await service.SearchAsync(Search(keyword: created.ProductCode), CancellationToken.None);
        var trash = await service.SearchTrashAsync(Search(keyword: created.ProductCode), CancellationToken.None);
        var detail = await service.GetByIdAsync(created.Id, CancellationToken.None);

        Assert.False(restored.IsDeleted);
        Assert.Single(list.Items);
        Assert.Empty(trash.Items);
        Assert.Equal(created.Id, detail.Id);
    }

    [Fact]
    public async Task Permanent_delete_removes_soft_deleted_product_but_keeps_audit_log()
    {
        var categoryId = await SeedCategoryAsync("PD", 1);
        var service = Get<IProductService>();
        var created = await service.CreateAsync(CreateRequest(categoryId, "Permanent delete product"), CancellationToken.None);
        await service.DeleteAsync(created.Id, CancellationToken.None);

        await service.DeletePermanentAsync(created.Id, CancellationToken.None);

        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Products.IgnoreQueryFilters().AnyAsync(x => x.Id == created.Id));

        var logs = await Get<IAuditLogService>().SearchAsync(new AuditLogSearchRequest
        {
            EntityType = "PRODUCT",
            EntityId = created.Id.ToString(),
            PageSize = 20
        }, CancellationToken.None);

        Assert.Contains(logs.Items, x => x.Action == "PERMANENT_DELETE" && x.EntityCode == created.ProductCode);
    }

    [Fact]
    public async Task Audit_log_records_product_create_update_delete_and_restore()
    {
        var categoryId = await SeedCategoryAsync("AU", 1);
        var productService = Get<IProductService>();
        var auditService = Get<IAuditLogService>();
        var created = await productService.CreateAsync(CreateRequest(categoryId, "Audit product", price: 10m), CancellationToken.None);
        await productService.UpdateAsync(created.Id, new ProductUpdateRequest
        {
            ProductName = "Audit product updated",
            Unit = "kg",
            Price = 11.50m,
            Description = "Changed",
            IsActive = true
        }, CancellationToken.None);
        await productService.DeleteAsync(created.Id, CancellationToken.None);
        await productService.RestoreAsync(created.Id, CancellationToken.None);

        var history = await productService.GetHistoryAsync(created.Id, page: 1, pageSize: 20, cancellationToken: CancellationToken.None);
        var byCode = await auditService.SearchAsync(new AuditLogSearchRequest
        {
            EntityType = "PRODUCT",
            EntityCode = created.ProductCode,
            PageSize = 20
        }, CancellationToken.None);

        Assert.Contains(history.Items, x => x.Action == "CREATE" && x.NewValues is not null);
        Assert.Contains(history.Items, x => x.Action == "UPDATE" && x.OldValues is not null && x.NewValues is not null && x.ChangedFields is not null);
        Assert.Contains(history.Items, x => x.Action == "DELETE" && x.OldValues is not null);
        Assert.Contains(history.Items, x => x.Action == "RESTORE" && x.NewValues is not null);
        Assert.Equal(history.TotalItems, byCode.TotalItems);
    }

    private async Task<int> SeedCategoryAsync(string prefix, int nextProductNumber, bool isActive = true)
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = new Category
        {
            CategoryCode = $"CAT-{prefix}",
            CategoryName = $"Category {prefix}",
            CodePrefix = prefix,
            NextProductNumber = nextProductNumber,
            IsActive = isActive,
            CreatedByUserId = "test-admin-id",
            IsAdminProtected = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Categories.Add(entity);
        await db.SaveChangesAsync();
        return entity.Id;
    }

    private T Get<T>() where T : notnull
        => _scope.ServiceProvider.GetRequiredService<T>();

    private async Task<string> ReadStoredProductNameAsync(int productId)
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Products
            .Where(x => x.Id == productId)
            .Select(x => x.ProductName)
            .SingleAsync();
    }

    private static ProductCreateRequest CreateRequest(
        int categoryId,
        string name,
        decimal price = 10m)
        => new()
        {
            ProductName = name,
            CategoryId = categoryId,
            Unit = "kg",
            Price = price,
            Description = $"{name} description",
            IsActive = true
        };

    private static ProductSearchRequest Search(
        string? keyword = null,
        decimal? minPrice = null,
        decimal? maxPrice = null)
        => new()
        {
            Keyword = keyword,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Page = 1,
            PageSize = 20,
            SortBy = "productCode",
            SortDirection = "asc"
        };
}
