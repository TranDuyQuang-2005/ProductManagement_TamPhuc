using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.DependencyInjection;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.DTOs.Products;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Tests.Support;

namespace ProductManagement.Api.Tests;

public sealed class ProductHttpApiIntegrationTests : IClassFixture<SqlServerTestDatabaseFixture>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        PropertyNamingPolicy = null
    };

    private readonly SqlServerTestDatabaseFixture _database;
    private ProductApiWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public ProductHttpApiIntegrationTests(SqlServerTestDatabaseFixture database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        await _database.ResetAsync();
        _factory = new ProductApiWebApplicationFactory(_database);
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Post_valid_product_returns_201()
    {
        var categoryId = await SeedCategoryAsync("HP", 1);
        Assert.True(categoryId > 0, $"Expected seeded category id > 0, got {categoryId}.");

        var response = await PostProductAsync(CreatePayload(categoryId, "HTTP valid product"));

        await AssertStatusAsync(HttpStatusCode.Created, response);
        var product = await ReadDataAsync<ProductResponse>(response);
        Assert.NotNull(product);
        Assert.Equal("HP000001", product.ProductCode);
        Assert.Equal("HTTP valid product", product.ProductName);
    }

    [Theory]
    [InlineData("", "kg", "1")]
    [InlineData("   ", "kg", "1")]
    [InlineData("Invalid price", "kg", "-0.01")]
    [InlineData("Invalid precision price", "kg", "1.001")]
    public async Task Post_invalid_validation_returns_400(
        string productName,
        string unit,
        string price)
    {
        var categoryId = await SeedCategoryAsync("HV", 1);

        var response = await PostProductAsync(new ProductCreateRequest
        {
            ProductName = productName,
            CategoryId = categoryId,
            Unit = unit,
            Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture),
            IsActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Zero price", "0")]
    [InlineData("Cent price", "0.01")]
    public async Task Post_accepts_zero_and_cent_boundaries(string name, string price)
    {
        var categoryId = await SeedCategoryAsync("HB", 1);

        var response = await PostProductAsync(new ProductCreateRequest
        {
            ProductName = name,
            CategoryId = categoryId,
            Unit = "kg",
            Price = decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture),
            IsActive = true
        });

        await AssertStatusAsync(HttpStatusCode.Created, response);
    }

    [Fact]
    public async Task Post_unknown_category_returns_400()
    {
        var response = await PostProductAsync(CreatePayload(categoryId: 999_999, "Unknown category"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Post_inactive_category_returns_400()
    {
        var categoryId = await SeedCategoryAsync("HI", 1, isActive: false);

        var response = await PostProductAsync(CreatePayload(categoryId, "Inactive category"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_products_supports_keyword_price_stock_active_and_combined_filters()
    {
        var categoryId = await SeedCategoryAsync("GS", 1);
        var otherCategoryId = await SeedCategoryAsync("GO", 1);
        var caPhe = await CreateProductAsync(categoryId, "Cà phê sữa đá", price: 10m, isActive: true);
        var caCao = await CreateProductAsync(categoryId, "Ca cao nóng", price: 25m, isActive: false);
        var traSen = await CreateProductAsync(otherCategoryId, "Trà sen", price: 40m, isActive: true);
        await StockInAsync(caPhe.Id, 5m);
        await StockInAsync(traSen.Id, 8m);

        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString(caPhe.ProductCode)}"), caPhe.Id);
        AssertContains(await GetProductsAsync("keyword=GS000"), caPhe.Id, caCao.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("Cà phê")}"), caPhe.Id);
        Assert.Empty((await GetProductsAsync("keyword=khong-ton-tai")).Items);
        AssertContains(await GetProductsAsync("minPrice=20"), caCao.Id);
        AssertContains(await GetProductsAsync("maxPrice=15"), caPhe.Id);
        AssertContainsOnly(await GetProductsAsync("minPrice=25&maxPrice=25"), caCao.Id);
        AssertContains(await GetProductsAsync("stockStatus=1"), caPhe.Id);
        AssertContains(await GetProductsAsync("stockStatus=2"), caCao.Id);
        AssertContains(await GetProductsAsync("isActive=false"), caCao.Id);
        AssertContainsOnly(await GetProductsAsync($"categoryId={categoryId}&minPrice=20&maxPrice=30&stockStatus=2&isActive=false"), caCao.Id);
    }

    [Fact]
    public async Task Get_products_uses_full_text_name_search_when_available_otherwise_prefix_fallback()
    {
        var categoryId = await SeedCategoryAsync("FT", 1);
        var product = await CreateProductAsync(categoryId, "Alpha Vietnamese Coffee", price: 10m);

        if (await IsProductNameFullTextEnabledAsync())
        {
            AssertContainsOnly(await WaitForProductsAsync("keyword=Vietnamese", product.Id), product.Id);
        }
        else
        {
            AssertContainsOnly(await GetProductsAsync("keyword=Alpha"), product.Id);
        }
    }

    [Fact]
    public async Task Get_products_keyword_bo_uses_full_text_when_available()
    {
        var categoryId = await SeedCategoryAsync("VN", 1);
        var middleMatch = await CreateProductAsync(categoryId, "Th\u1ecbt b\u00f2 M\u1ef9", price: 10m);
        var prefixFallback = await CreateProductAsync(categoryId, "b\u00f2 kho", price: 12m);
        var query = $"keyword={Uri.EscapeDataString("b\u00f2")}";

        var result = await IsProductNameFullTextEnabledAsync()
            ? await WaitForProductsAsync(query, middleMatch.Id)
            : await GetProductsAsync(query);

        AssertContains(result, prefixFallback.Id);
        if (await IsProductNameFullTextEnabledAsync())
            AssertContains(result, middleMatch.Id);
    }

    [Fact]
    public async Task Get_products_keyword_with_percent_does_not_use_full_text_term()
    {
        var categoryId = await SeedCategoryAsync("VK", 1);
        var fullTextOnlyMatch = await CreateProductAsync(categoryId, "Th\u1ecbt b\u00f2 M\u1ef9", price: 10m);
        var literalPrefixMatch = await CreateProductAsync(categoryId, "b\u00f2% literal", price: 12m);

        var result = await GetProductsAsync($"keyword={Uri.EscapeDataString("b\u00f2%")}");

        AssertContains(result, literalPrefixMatch.Id);
        Assert.DoesNotContain(result.Items, x => x.Id == fullTextOnlyMatch.Id);
    }

    [Fact]
    public async Task Get_products_literal_name_search_matches_punctuation_as_literal_contains()
    {
        var categoryId = await SeedCategoryAsync("LX", 1);
        var percent = await CreateProductAsync(categoryId, "N\u01b0\u1edbc 10% \u0111\u01b0\u1eddng", price: 10m);
        var noPercent = await CreateProductAsync(categoryId, "N\u01b0\u1edbc 10 \u0111\u01b0\u1eddng", price: 11m);
        var underscore = await CreateProductAsync(categoryId, "M\u00e3_h\u00e0ng literal", price: 12m);
        var bracket = await CreateProductAsync(categoryId, "B\u1ed9 [test] literal", price: 13m);
        var tilde = await CreateProductAsync(categoryId, "Tilde ~ literal", price: 14m);
        var cpp = await CreateProductAsync(categoryId, "S\u00e1ch C++ c\u01a1 b\u1ea3n", price: 15m);
        var slash = await CreateProductAsync(categoryId, "Adapter A/B", price: 16m);
        var apostrophe = await CreateProductAsync(categoryId, "S\u00e1ch O'Reilly", price: 17m);
        var injection = await CreateProductAsync(categoryId, "Literal ' OR 1=1 -- marker", price: 18m);

        var tenPercent = await GetProductsAsync($"keyword={Uri.EscapeDataString("10%")}");
        AssertContains(tenPercent, percent.Id);
        Assert.DoesNotContain(tenPercent.Items, x => x.Id == noPercent.Id);

        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("%")}"), percent.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("_")}"), underscore.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("[")}"), bracket.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("~")}"), tilde.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("C++")}"), cpp.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("A/B")}"), slash.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("O'Reilly")}"), apostrophe.Id);
        AssertContainsOnly(await GetProductsAsync($"keyword={Uri.EscapeDataString("' OR 1=1 --")}"), injection.Id);
    }

    [Theory]
    [InlineData("'")]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("[")]
    [InlineData("~")]
    [InlineData("' OR 1=1 --")]
    public async Task Get_products_keyword_special_characters_do_not_error_or_bypass_query(string keyword)
    {
        var categoryId = await SeedCategoryAsync("GI", 1);
        await CreateProductAsync(categoryId, "Normal product", price: 10m);

        var response = await _client.PostAsJsonAsync("/api/products/search", SearchRequest($"keyword={Uri.EscapeDataString(keyword)}"));

        await AssertStatusAsync(HttpStatusCode.OK, response);
        var result = await ReadDataAsync<PagedResult<ProductResponse>>(response);
        Assert.NotNull(result);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task Get_products_min_price_greater_than_max_price_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/products/search", SearchRequest("minPrice=100&maxPrice=10"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_products_pagination_boundaries_work()
    {
        var categoryId = await SeedCategoryAsync("PG", 1);
        for (var index = 1; index <= 23; index++)
            await CreateProductAsync(categoryId, $"Paged {index:00}", price: index);

        var firstPage = await GetProductsAsync("page=1&pageSize=10&sortBy=productCode&sortDirection=asc");
        var lastPage = await GetProductsAsync("page=3&pageSize=10&sortBy=productCode&sortDirection=asc");
        var beyondPage = await GetProductsAsync("page=4&pageSize=10&sortBy=productCode&sortDirection=asc");
        var pageSize100 = await GetProductsAsync("page=1&pageSize=100");

        Assert.Equal(23, firstPage.TotalItems);
        Assert.Equal(3, firstPage.TotalPages);
        Assert.Equal(10, firstPage.Items.Count);
        Assert.Equal(3, lastPage.Items.Count);
        Assert.Empty(beyondPage.Items);
        Assert.Equal(23, pageSize100.Items.Count);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/products/search", SearchRequest("page=0"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/products/search", SearchRequest("pageSize=0"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/products/search", SearchRequest("pageSize=101"))).StatusCode);
    }

    [Fact]
    public async Task Get_products_sorting_and_invalid_sort_validation_work()
    {
        var categoryId = await SeedCategoryAsync("SO", 1);
        var low = await CreateProductAsync(categoryId, "Bravo", price: 5m);
        var high = await CreateProductAsync(categoryId, "Alpha", price: 15m);

        var priceAsc = await GetProductsAsync("sortBy=price&sortDirection=asc&pageSize=10");
        var priceDesc = await GetProductsAsync("sortBy=price&sortDirection=desc&pageSize=10");
        var nameAsc = await GetProductsAsync("sortBy=productName&sortDirection=asc&pageSize=10");

        Assert.Equal(low.Id, priceAsc.Items[0].Id);
        Assert.Equal(high.Id, priceDesc.Items[0].Id);
        Assert.Equal(high.Id, nameAsc.Items[0].Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/products/search", SearchRequest("sortBy=invalid"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.PostAsJsonAsync("/api/products/search", SearchRequest("sortDirection=sideways"))).StatusCode);
    }

    [Fact]
    public async Task Put_product_validates_and_keeps_code_and_category()
    {
        var categoryId = await SeedCategoryAsync("PU", 1);
        var otherCategoryId = await SeedCategoryAsync("PX", 1);
        var created = await CreateProductAsync(categoryId, "Before update", price: 10m);

        var updateResponse = await _client.PostAsJsonAsync("/api/products/update", new
        {
            id = created.Id,
            productCode = "CLIENT-SHOULD-NOT-WIN",
            categoryId = otherCategoryId,
            productName = "After update",
            unit = "box",
            price = 22.25m,
            description = "Updated via HTTP",
            isActive = false
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await ReadDataAsync<ProductResponse>(updateResponse);
        Assert.NotNull(updated);
        Assert.Equal(created.ProductCode, updated.ProductCode);
        Assert.Equal(categoryId, updated.CategoryId);
        Assert.Equal("After update", updated.ProductName);
        Assert.False(updated.IsActive);

        Assert.Equal(HttpStatusCode.NotFound, (await UpdateProductAsync(999999, UpdatePayload("Missing"))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UpdateProductAsync(created.Id, UpdatePayload("Bad price", price: -0.01m))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await UpdateProductAsync(created.Id, UpdatePayload("   "))).StatusCode);
    }

    [Fact]
    public async Task Product_lifecycle_http_flow_preserves_soft_delete_restore_permanent_delete_and_audit()
    {
        var categoryId = await SeedCategoryAsync("LF", 1);
        var created = await CreateProductAsync(categoryId, "Lifecycle", price: 10m);

        var update = await UpdateProductAsync(created.Id, UpdatePayload("Lifecycle updated", isActive: false));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var inactive = await ReadDataAsync<ProductResponse>(update);
        Assert.NotNull(inactive);
        Assert.False(inactive.IsActive);
        Assert.False(inactive.IsDeleted);

        var activate = await UpdateProductAsync(created.Id, UpdatePayload("Lifecycle updated", isActive: true));
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);
        Assert.True((await ReadDataAsync<ProductResponse>(activate))!.IsActive);

        Assert.Equal(HttpStatusCode.OK, (await DeleteProductAsync(created.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetProductAsync(created.Id)).StatusCode);
        Assert.Empty((await GetProductsAsync($"keyword={created.ProductCode}")).Items);
        Assert.Single((await GetTrashAsync($"keyword={created.ProductCode}")).Items);

        var restore = await RestoreProductAsync(created.Id);
        Assert.Equal(HttpStatusCode.OK, restore.StatusCode);
        Assert.Single((await GetProductsAsync($"keyword={created.ProductCode}")).Items);
        Assert.Equal(HttpStatusCode.OK, (await GetProductAsync(created.Id)).StatusCode);
        Assert.Empty((await GetTrashAsync($"keyword={created.ProductCode}")).Items);

        Assert.Equal(HttpStatusCode.OK, (await DeleteProductAsync(created.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DeletePermanentProductAsync(created.Id)).StatusCode);

        await using var connection = new SqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var productCommand = connection.CreateCommand();
        productCommand.CommandText = "SELECT COUNT(1) FROM dbo.Products WHERE Id = @Id";
        productCommand.Parameters.AddWithValue("@Id", created.Id);
        Assert.Equal(0, ToInt32(await productCommand.ExecuteScalarAsync()));

        await using var auditCommand = connection.CreateCommand();
        auditCommand.CommandText = "SELECT COUNT(1) FROM dbo.AuditLogs WHERE EntityType = N'PRODUCT' AND EntityId = @Id";
        auditCommand.Parameters.AddWithValue("@Id", created.Id.ToString());
        Assert.True(ToInt32(await auditCommand.ExecuteScalarAsync()) >= 5);
    }

    [Fact]
    public async Task Soft_delete_and_restore_update_database_delete_metadata()
    {
        var categoryId = await SeedCategoryAsync("DB", 1);
        var created = await CreateProductAsync(categoryId, "Database metadata");

        Assert.Equal(HttpStatusCode.OK, (await DeleteProductAsync(created.Id)).StatusCode);
        var deleted = await ReadProductRowAsync(created.Id);
        Assert.NotNull(deleted);
        Assert.True(deleted.IsDeleted);
        Assert.NotNull(deleted.DeletedAt);
        Assert.NotNull(deleted.DeletedByUserId);
        Assert.NotNull(deleted.DeletedByUsername);

        Assert.Equal(HttpStatusCode.OK, (await RestoreProductAsync(created.Id)).StatusCode);
        var restored = await ReadProductRowAsync(created.Id);
        Assert.NotNull(restored);
        Assert.False(restored.IsDeleted);
        Assert.Null(restored.DeletedAt);
        Assert.Null(restored.DeletedByUserId);
        Assert.Null(restored.DeletedByUsername);
    }

    [Fact]
    public async Task Permanent_delete_only_allows_soft_deleted_products_and_keeps_audit()
    {
        var categoryId = await SeedCategoryAsync("PM", 1);
        var created = await CreateProductAsync(categoryId, "Permanent HTTP");

        Assert.Equal(HttpStatusCode.Conflict, (await DeletePermanentProductAsync(created.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DeleteProductAsync(created.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await DeletePermanentProductAsync(created.Id)).StatusCode);

        Assert.Null(await ReadProductRowAsync(created.Id));
        var audit = await GetAuditAsync(created.Id);
        Assert.Contains(audit.Items, x => x.Action == "PERMANENT_DELETE" && x.EntityCode == created.ProductCode);
    }

    [Fact]
    public async Task Audit_detail_records_actions_snapshots_and_only_changed_update_fields()
    {
        var categoryId = await SeedCategoryAsync("AD", 1);
        var created = await CreateProductAsync(categoryId, "Audit detail", price: 10m);
        await UpdateProductAsync(created.Id, UpdatePayload("Audit detail updated", price: 11m, description: "Only name price description"));
        await DeleteProductAsync(created.Id);
        await RestoreProductAsync(created.Id);
        await DeleteProductAsync(created.Id);
        await DeletePermanentProductAsync(created.Id);

        var audit = await GetAuditAsync(created.Id);

        Assert.Contains(audit.Items, x => x.Action == "CREATE" && x.EntityType == "PRODUCT" && x.EntityCode == created.ProductCode);
        var update = Assert.Single(audit.Items, x => x.Action == "UPDATE");
        Assert.Equal("PRODUCT", update.EntityType);
        Assert.Equal(created.ProductCode, update.EntityCode);
        Assert.NotNull(update.OldValues);
        Assert.NotNull(update.NewValues);
        var changedFields = JsonSerializer.Deserialize<string[]>(update.ChangedFields!, JsonOptions);
        Assert.NotNull(changedFields);
        Assert.Equal(["ProductName", "Price", "Description"], changedFields);
        Assert.Contains(audit.Items, x => x.Action == "DELETE");
        Assert.Contains(audit.Items, x => x.Action == "RESTORE");
        Assert.Contains(audit.Items, x => x.Action == "PERMANENT_DELETE");
    }

    [Fact]
    public async Task Product_code_multiple_and_concurrent_creates_are_unique_and_increment_counter()
    {
        var categoryId = await SeedCategoryAsync("CC", 1);

        var sequential = new List<ProductResponse>();
        for (var index = 0; index < 5; index++)
            sequential.Add(await CreateProductAsync(categoryId, $"Sequential {index}", price: index + 1));

        Assert.Equal(["CC000001", "CC000002", "CC000003", "CC000004", "CC000005"], sequential.Select(x => x.ProductCode).ToArray());

        var concurrent = await Task.WhenAll(
            CreateProductAsync(categoryId, "Concurrent A", price: 11m),
            CreateProductAsync(categoryId, "Concurrent B", price: 12m));

        var allCodes = sequential.Concat(concurrent).Select(x => x.ProductCode).ToArray();
        Assert.Equal(allCodes.Length, allCodes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(allCodes, code => Assert.Matches("^CC[0-9]{6}$", code));

        await using var connection = new SqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT NextProductNumber FROM dbo.Categories WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", categoryId);
        Assert.Equal(8, ToInt32(await command.ExecuteScalarAsync()));
    }

    private async Task<HttpResponseMessage> PostProductAsync(object payload)
        => await _client.PostAsJsonAsync("/api/products/create", payload, RequestJsonOptions);

    private async Task<ProductResponse> CreateProductAsync(
        int categoryId,
        string name,
        decimal price = 10m,
        bool isActive = true)
    {
        var response = await PostProductAsync(CreatePayload(categoryId, name, price, isActive));
        await AssertStatusAsync(HttpStatusCode.Created, response);
        return (await ReadDataAsync<ProductResponse>(response))!;
    }

    private async Task<HttpResponseMessage> GetProductAsync(int id)
        => await _client.PostAsJsonAsync("/api/products/get", new { id });

    private async Task<HttpResponseMessage> UpdateProductAsync(int id, ProductUpdateRequest payload)
        => await _client.PostAsJsonAsync("/api/products/update", new
        {
            id,
            payload.ProductName,
            payload.Unit,
            payload.Price,
            payload.Description,
            payload.IsActive
        });

    private async Task<HttpResponseMessage> DeleteProductAsync(int id)
        => await _client.PostAsJsonAsync("/api/products/delete", new { id });

    private async Task<HttpResponseMessage> RestoreProductAsync(int id)
        => await _client.PostAsJsonAsync("/api/products/restore", new { id });

    private async Task<HttpResponseMessage> DeletePermanentProductAsync(int id)
        => await _client.PostAsJsonAsync("/api/products/delete-permanent", new { id });

    private async Task<HttpResponseMessage> StockInAsync(int productId, decimal quantity)
        => await _client.PostAsJsonAsync("/api/inventory/stock-in", new { productId, quantity });

    private async Task<PagedResult<ProductResponse>> GetProductsAsync(string query = "")
    {
        var response = await _client.PostAsJsonAsync("/api/products/search", SearchRequest(query));
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await ReadDataAsync<PagedResult<ProductResponse>>(response))!;
    }

    private async Task<PagedResult<ProductResponse>> WaitForProductsAsync(string query, int expectedProductId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        PagedResult<ProductResponse>? lastResult = null;

        do
        {
            lastResult = await GetProductsAsync(query);
            if (lastResult.Items.Any(x => x.Id == expectedProductId))
                return lastResult;

            await Task.Delay(250);
        }
        while (DateTime.UtcNow < deadline);

        return lastResult ?? await GetProductsAsync(query);
    }

    private async Task<PagedResult<ProductResponse>> GetTrashAsync(string query = "")
    {
        var response = await _client.PostAsJsonAsync("/api/products/trash/search", SearchRequest(query));
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await ReadDataAsync<PagedResult<ProductResponse>>(response))!;
    }

    private async Task<PagedResult<AuditLogResponse>> GetAuditAsync(int productId)
    {
        var response = await _client.PostAsJsonAsync("/api/audit-logs/search", new AuditLogSearchRequest
        {
            EntityType = "PRODUCT",
            EntityId = productId.ToString(),
            Page = 1,
            PageSize = 50
        });
        await AssertStatusAsync(HttpStatusCode.OK, response);
        return (await ReadDataAsync<PagedResult<AuditLogResponse>>(response))!;
    }

    private static ProductSearchRequest SearchRequest(string query = "")
    {
        string? keyword = null;
        int? categoryId = null;
        decimal? minPrice = null;
        decimal? maxPrice = null;
        var stockStatus = StockStatusFilter.All;
        bool? isActive = null;
        var page = 1;
        var pageSize = 10;
        var sortBy = "createdAt";
        var sortDirection = "desc";

        foreach (var pair in (query ?? string.Empty).Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;

            switch (key)
            {
                case "keyword":
                    keyword = value;
                    break;
                case "categoryId":
                    categoryId = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "minPrice":
                    minPrice = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "maxPrice":
                    maxPrice = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "stockStatus":
                    stockStatus = ParseStockStatus(value);
                    break;
                case "isActive":
                    isActive = bool.Parse(value);
                    break;
                case "page":
                    page = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "pageSize":
                    pageSize = int.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "sortBy":
                    sortBy = value;
                    break;
                case "sortDirection":
                    sortDirection = value;
                    break;
            }
        }

        return new ProductSearchRequest
        {
            Keyword = keyword,
            CategoryId = categoryId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            StockStatus = stockStatus,
            IsActive = isActive,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };
    }

    private static StockStatusFilter ParseStockStatus(string value)
        => value switch
        {
            "1" => StockStatusFilter.InStock,
            "2" => StockStatusFilter.OutOfStock,
            _ when Enum.TryParse<StockStatusFilter>(value, ignoreCase: true, out var parsed) => parsed,
            _ => StockStatusFilter.All
        };

    private static async Task<T?> ReadDataAsync<T>(HttpResponseMessage response)
    {
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(JsonOptions);
        return apiResponse is null ? default : apiResponse.Data;
    }

    private static async Task AssertStatusAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        if (response.StatusCode == expected) return;

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Expected HTTP {(int)expected} {expected}, got {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
    }

    private async Task<int> SeedCategoryAsync(string prefix, int nextProductNumber, bool isActive = true)
    {
        await using var connection = new SqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DECLARE @Id int = ISNULL((SELECT MAX(Id) FROM dbo.Categories), 0) + 1;

            IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Categories'), N'Id', 'IsIdentity') = 1
                SET IDENTITY_INSERT dbo.Categories ON;

            INSERT dbo.Categories
                (Id, CategoryCode, CategoryName, CodePrefix, NextProductNumber, IsActive,
                 CreatedByUserId, IsAdminProtected, CreatedAt)
            VALUES
                (@Id, @CategoryCode, @CategoryName, @CodePrefix, @NextProductNumber, @IsActive,
                 @CreatedByUserId, 1, SYSUTCDATETIME());

            IF COLUMNPROPERTY(OBJECT_ID(N'dbo.Categories'), N'Id', 'IsIdentity') = 1
                SET IDENTITY_INSERT dbo.Categories OFF;

            SELECT @Id;
            """;
        command.Parameters.AddWithValue("@CategoryCode", $"HTTP-{prefix}");
        command.Parameters.AddWithValue("@CategoryName", $"HTTP Category {prefix}");
        command.Parameters.AddWithValue("@CodePrefix", prefix);
        command.Parameters.AddWithValue("@NextProductNumber", nextProductNumber);
        command.Parameters.AddWithValue("@IsActive", isActive);
        command.Parameters.AddWithValue("@CreatedByUserId", "test-admin-id");
        return ToInt32(await command.ExecuteScalarAsync());
    }

    private async Task<ProductRow?> ReadProductRowAsync(int productId)
    {
        await using var connection = new SqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.IsDeleted, p.DeletedAt, p.DeletedByUserId, u.UserName AS DeletedByUsername
            FROM dbo.Products
            p LEFT JOIN dbo.AspNetUsers u ON u.Id = p.DeletedByUserId
            WHERE p.Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", productId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new ProductRow(
            reader.GetBoolean(0),
            reader.IsDBNull(1) ? null : reader.GetDateTime(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    private async Task<bool> IsProductNameFullTextEnabledAsync()
    {
        await using var connection = new SqlConnection(_database.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CAST(CASE
                WHEN ISNULL(FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'), 0) = 1
                 AND EXISTS (
                    SELECT 1
                    FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID(N'dbo.Products')
                 )
                THEN 1 ELSE 0 END AS bit);
            """;

        return (bool)(await command.ExecuteScalarAsync()
            ?? throw new InvalidOperationException("Expected full-text availability scalar."));
    }

    private static ProductCreateRequest CreatePayload(
        int categoryId,
        string name,
        decimal price = 10m,
        bool isActive = true)
        => new ProductCreateRequest
        {
            ProductName = name,
            CategoryId = categoryId,
            Unit = "kg",
            Price = price,
            Description = $"{name} description",
            IsActive = isActive
        };

    private static ProductUpdateRequest UpdatePayload(
        string name,
        decimal price = 10m,
        string? description = "Updated",
        bool isActive = true)
        => new ProductUpdateRequest
        {
            ProductName = name,
            Unit = "kg",
            Price = price,
            Description = description,
            IsActive = isActive
        };

    private static void AssertContains(PagedResult<ProductResponse> result, params int[] productIds)
    {
        foreach (var productId in productIds)
            Assert.Contains(result.Items, x => x.Id == productId);
    }

    private static void AssertContainsOnly(PagedResult<ProductResponse> result, int productId)
    {
        var item = Assert.Single(result.Items);
        Assert.Equal(productId, item.Id);
    }

    private sealed record ProductRow(
        bool IsDeleted,
        DateTime? DeletedAt,
        string? DeletedByUserId,
        string? DeletedByUsername);

    private static int ToInt32(object? value)
        => value is null or DBNull
            ? throw new InvalidOperationException("Expected SQL scalar value.")
            : Convert.ToInt32(value, System.Globalization.CultureInfo.InvariantCulture);
}
