using Dapper;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.Products;
using ProductManagement.Api.Repositories.Interfaces;

namespace ProductManagement.Api.Repositories;

public sealed class ProductQueryRepository(DapperContext context) : IProductQueryRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["productCode"] = "p.ProductCode",
            ["productName"] = "p.ProductName",
            ["categoryName"] = "c.CategoryName",
            ["price"] = "p.Price",
            ["quantity"] = "p.Quantity",
            ["createdAt"] = "p.CreatedAt"
        };

    public async Task<PagedResult<ProductResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            where.Add("(CHARINDEX(@Keyword, p.ProductCode) > 0 OR CHARINDEX(@Keyword, p.ProductName) > 0)");
            parameters.Add("Keyword", request.Keyword.Trim());
        }

        if (request.CategoryId.HasValue)
        {
            where.Add("p.CategoryId = @CategoryId");
            parameters.Add("CategoryId", request.CategoryId.Value);
        }

        if (request.MinPrice.HasValue)
        {
            where.Add("p.Price >= @MinPrice");
            parameters.Add("MinPrice", request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            where.Add("p.Price <= @MaxPrice");
            parameters.Add("MaxPrice", request.MaxPrice.Value);
        }

        if (request.IsActive.HasValue)
        {
            where.Add("p.IsActive = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        switch (request.StockStatus)
        {
            case StockStatusFilter.InStock:
                where.Add("p.Quantity > 0");
                break;
            case StockStatusFilter.OutOfStock:
                where.Add("p.Quantity = 0");
                break;
        }

        var sortColumn = SortColumns.TryGetValue(request.SortBy, out var mapped) ? mapped : SortColumns["createdAt"];
        var sortDirection = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        var offset = (request.Page - 1) * request.PageSize;
        parameters.Add("Offset", offset);
        parameters.Add("PageSize", request.PageSize);

        var whereSql = string.Join(" AND ", where);
        var sql = $"""
            SELECT
                p.Id,
                p.ProductCode,
                p.ProductName,
                p.CategoryId,
                c.CategoryCode,
                c.CategoryName,
                c.IsActive AS CategoryIsActive,
                p.Unit,
                p.Price,
                p.Quantity,
                CASE WHEN p.Quantity > 0 THEN N'Còn hàng' ELSE N'Hết hàng' END AS StockStatus,
                p.Description,
                p.IsActive,
                p.CreatedByUserId,
                p.CreatedByUsername,
                p.CreatedByRole,
                p.LastModifiedByUserId,
                p.LastModifiedByUsername,
                p.LastModifiedByRole,
                CAST(CASE WHEN UPPER(ISNULL(p.CreatedByRole, N'')) <> N'STAFF' OR (p.LastModifiedByRole IS NOT NULL AND UPPER(p.LastModifiedByRole) <> N'STAFF') THEN 1 ELSE 0 END AS bit) AS IsAdminProtected,
                p.CreatedAt,
                p.UpdatedAt
            FROM dbo.Products p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            WHERE {whereSql}
            ORDER BY {sortColumn} {sortDirection}, p.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.Products p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            WHERE {whereSql};
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var grid = await connection.QueryMultipleAsync(command);
        var items = (await grid.ReadAsync<ProductResponse>()).AsList();
        var totalItems = await grid.ReadSingleAsync<int>();

        return PagedResult<ProductResponse>.Create(items, request.Page, request.PageSize, totalItems);
    }

    public async Task<ProductResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                p.Id,
                p.ProductCode,
                p.ProductName,
                p.CategoryId,
                c.CategoryCode,
                c.CategoryName,
                c.IsActive AS CategoryIsActive,
                p.Unit,
                p.Price,
                p.Quantity,
                CASE WHEN p.Quantity > 0 THEN N'Còn hàng' ELSE N'Hết hàng' END AS StockStatus,
                p.Description,
                p.IsActive,
                p.CreatedByUserId,
                p.CreatedByUsername,
                p.CreatedByRole,
                p.LastModifiedByUserId,
                p.LastModifiedByUsername,
                p.LastModifiedByRole,
                CAST(CASE WHEN UPPER(ISNULL(p.CreatedByRole, N'')) <> N'STAFF' OR (p.LastModifiedByRole IS NOT NULL AND UPPER(p.LastModifiedByRole) <> N'STAFF') THEN 1 ELSE 0 END AS bit) AS IsAdminProtected,
                p.CreatedAt,
                p.UpdatedAt
            FROM dbo.Products p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            WHERE p.Id = @Id;
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ProductResponse>(command);
    }
}
