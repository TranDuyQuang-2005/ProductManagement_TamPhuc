using Dapper;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.Categories;
using ProductManagement.Api.Repositories.Interfaces;

namespace ProductManagement.Api.Repositories;

public sealed class CategoryQueryRepository(DapperContext context) : ICategoryQueryRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["categoryCode"] = "c.CategoryCode",
            ["categoryName"] = "c.CategoryName",
            ["productCount"] = "ProductCount",
            ["createdAt"] = "c.CreatedAt"
        };

    public async Task<PagedResult<CategoryResponse>> SearchAsync(CategorySearchRequest request, CancellationToken cancellationToken)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            where.Add("(CHARINDEX(@Keyword, c.CategoryCode) > 0 OR CHARINDEX(@Keyword, c.CategoryName) > 0)");
            parameters.Add("Keyword", request.Keyword.Trim());
        }

        if (request.IsActive.HasValue)
        {
            where.Add("c.IsActive = @IsActive");
            parameters.Add("IsActive", request.IsActive.Value);
        }

        var sortColumn = SortColumns.TryGetValue(request.SortBy, out var mapped) ? mapped : SortColumns["createdAt"];
        var sortDirection = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var whereSql = string.Join(" AND ", where);
        var sql = $"""
            SELECT
                c.Id,
                c.CategoryCode,
                c.CategoryName,
                c.CodePrefix,
                c.NextProductNumber,
                c.Description,
                c.IsActive,
                COUNT(p.Id) AS ProductCount,
                CAST(CASE WHEN COUNT(p.Id) > 0 THEN 1 ELSE 0 END AS bit) AS HasProducts,
                CAST(CASE WHEN COUNT(p.Id) = 0 THEN 1 ELSE 0 END AS bit) AS CanEditPrefix,
                c.CreatedByUserId,
                c.CreatedByUsername,
                c.CreatedByRole,
                c.LastModifiedByUserId,
                c.LastModifiedByUsername,
                c.LastModifiedByRole,
                CAST(CASE WHEN UPPER(ISNULL(c.CreatedByRole, N'')) <> N'STAFF' OR (c.LastModifiedByRole IS NOT NULL AND UPPER(c.LastModifiedByRole) <> N'STAFF') THEN 1 ELSE 0 END AS bit) AS IsAdminProtected,
                c.CreatedAt,
                c.UpdatedAt
            FROM dbo.Categories c
            LEFT JOIN dbo.Products p ON p.CategoryId = c.Id
            WHERE {whereSql}
            GROUP BY c.Id, c.CategoryCode, c.CategoryName, c.CodePrefix, c.NextProductNumber, c.Description, c.IsActive,
                     c.CreatedByUserId, c.CreatedByUsername, c.CreatedByRole,
                     c.LastModifiedByUserId, c.LastModifiedByUsername, c.LastModifiedByRole,
                     c.CreatedAt, c.UpdatedAt
            ORDER BY {sortColumn} {sortDirection}, c.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.Categories c
            WHERE {whereSql};
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var grid = await connection.QueryMultipleAsync(command);
        var items = (await grid.ReadAsync<CategoryResponse>()).AsList();
        var totalItems = await grid.ReadSingleAsync<int>();
        return PagedResult<CategoryResponse>.Create(items, request.Page, request.PageSize, totalItems);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                c.Id,
                c.CategoryCode,
                c.CategoryName,
                c.CodePrefix,
                c.NextProductNumber,
                c.Description,
                c.IsActive,
                COUNT(p.Id) AS ProductCount,
                CAST(CASE WHEN COUNT(p.Id) > 0 THEN 1 ELSE 0 END AS bit) AS HasProducts,
                CAST(CASE WHEN COUNT(p.Id) = 0 THEN 1 ELSE 0 END AS bit) AS CanEditPrefix,
                c.CreatedByUserId,
                c.CreatedByUsername,
                c.CreatedByRole,
                c.LastModifiedByUserId,
                c.LastModifiedByUsername,
                c.LastModifiedByRole,
                CAST(CASE WHEN UPPER(ISNULL(c.CreatedByRole, N'')) <> N'STAFF' OR (c.LastModifiedByRole IS NOT NULL AND UPPER(c.LastModifiedByRole) <> N'STAFF') THEN 1 ELSE 0 END AS bit) AS IsAdminProtected,
                c.CreatedAt,
                c.UpdatedAt
            FROM dbo.Categories c
            LEFT JOIN dbo.Products p ON p.CategoryId = c.Id
            WHERE c.Id = @Id
            GROUP BY c.Id, c.CategoryCode, c.CategoryName, c.CodePrefix, c.NextProductNumber, c.Description, c.IsActive,
                     c.CreatedByUserId, c.CreatedByUsername, c.CreatedByRole,
                     c.LastModifiedByUserId, c.LastModifiedByUsername, c.LastModifiedByRole,
                     c.CreatedAt, c.UpdatedAt;
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CategoryResponse>(command);
    }

    public async Task<ProductCodePreviewResponse?> GetProductCodePreviewAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                Id AS CategoryId,
                CodePrefix,
                NextProductNumber,
                CONCAT(CodePrefix, RIGHT(CONCAT('000000', CONVERT(varchar(20), NextProductNumber)), 6)) AS ProductCodePreview
            FROM dbo.Categories
            WHERE Id = @Id;
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ProductCodePreviewResponse>(command);
    }

    public async Task<CategoryOptionResponse?> GetOptionByIdAsync(int id, CancellationToken cancellationToken)
    {
        const string sql = "SELECT Id, CategoryCode, CategoryName, CodePrefix, IsActive FROM dbo.Categories WHERE Id = @Id;";
        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CategoryOptionResponse>(command);
    }

    public async Task<IReadOnlyList<CategoryOptionResponse>> GetOptionsAsync(bool activeOnly, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, CategoryCode, CategoryName, CodePrefix, IsActive
            FROM dbo.Categories
            WHERE (@ActiveOnly = 0 OR IsActive = 1)
            ORDER BY CategoryName ASC, Id ASC;
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, new { ActiveOnly = activeOnly }, cancellationToken: cancellationToken);
        return (await connection.QueryAsync<CategoryOptionResponse>(command)).AsList();
    }
}
