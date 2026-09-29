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
            ["stockQuantity"] = "p.StockQuantity",
            ["createdAt"] = "p.CreatedAt"
        };

    public async Task<PagedResult<ProductResponse>> SearchAsync(ProductSearchRequest request, CancellationToken cancellationToken)
        => await SearchCoreAsync(request, isDeleted: false, cancellationToken);

    public async Task<PagedResult<ProductResponse>> SearchTrashAsync(ProductSearchRequest request, CancellationToken cancellationToken)
        => await SearchCoreAsync(request, isDeleted: true, cancellationToken);

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
                p.StockQuantity,
                CASE WHEN p.StockQuantity > 0 THEN N'Còn hàng' ELSE N'Hết hàng' END AS StockStatus,
                p.Description,
                p.IsActive,
                p.CreatedByUserId,
                createdUser.UserName AS CreatedByUsername,
                p.LastModifiedByUserId,
                modifiedUser.UserName AS LastModifiedByUsername,
                p.IsAdminProtected,
                p.IsDeleted,
                p.DeletedAt,
                p.DeletedByUserId,
                deletedUser.UserName AS DeletedByUsername,
                p.CreatedAt,
                p.UpdatedAt
            FROM dbo.Products p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            LEFT JOIN dbo.AspNetUsers createdUser ON createdUser.Id = p.CreatedByUserId
            LEFT JOIN dbo.AspNetUsers modifiedUser ON modifiedUser.Id = p.LastModifiedByUserId
            LEFT JOIN dbo.AspNetUsers deletedUser ON deletedUser.Id = p.DeletedByUserId
            WHERE p.Id = @Id AND p.IsDeleted = 0;
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<ProductResponse>(command);
    }

    private async Task<PagedResult<ProductResponse>> SearchCoreAsync(
        ProductSearchRequest request,
        bool isDeleted,
        CancellationToken cancellationToken)
    {
        var where = new List<string> { "p.IsDeleted = @IsDeleted" };
        var parameters = new DynamicParameters();
        parameters.Add("IsDeleted", isDeleted);

        await using var connection = context.CreateConnection();
        var keyword = TextNormalization.NormalizeNfcOrNull(request.Keyword);
        var requiresLiteralNameSearch = !string.IsNullOrEmpty(keyword) && RequiresLiteralNameSearch(keyword);
        var fullTextQuery = string.IsNullOrWhiteSpace(keyword) || requiresLiteralNameSearch
            ? null
            : BuildFullTextQuery(keyword);
        var useFullText = !requiresLiteralNameSearch
                          && !string.IsNullOrWhiteSpace(fullTextQuery)
                          && await IsProductNameFullTextEnabledAsync(connection, cancellationToken);
        var useNameContainsSearch = !string.IsNullOrWhiteSpace(keyword)
                                    && (requiresLiteralNameSearch || !useFullText);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var nameSearchSql = useFullText
                ? "(p.ProductName LIKE @KeywordPrefix ESCAPE N'~' OR ft.[KEY] IS NOT NULL)"
                : useNameContainsSearch
                    ? "p.ProductName LIKE @KeywordContains ESCAPE N'~'"
                    : "p.ProductName LIKE @KeywordPrefix ESCAPE N'~'";

            where.Add($"(p.ProductCode = @Keyword OR p.ProductCode LIKE @KeywordPrefix ESCAPE N'~' OR {nameSearchSql})");
            parameters.Add("Keyword", keyword);
            var escapedKeyword = EscapeLikePattern(keyword);
            parameters.Add("KeywordPrefix", escapedKeyword + "%");
            parameters.Add("KeywordContains", $"%{escapedKeyword}%");
            if (useFullText) parameters.Add("FullTextQuery", fullTextQuery);
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
                where.Add("p.StockQuantity > 0");
                break;
            case StockStatusFilter.OutOfStock:
                where.Add("p.StockQuantity = 0");
                break;
        }

        var sortColumn = SortColumns.TryGetValue(request.SortBy, out var mapped) ? mapped : SortColumns["createdAt"];
        var sortDirection = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);

        var whereSql = string.Join(" AND ", where);
        var fullTextJoin = useFullText
            ? "LEFT JOIN CONTAINSTABLE(dbo.Products, ProductName, @FullTextQuery) ft ON ft.[KEY] = p.Id"
            : string.Empty;
        var keywordOrder = string.IsNullOrWhiteSpace(keyword)
            ? string.Empty
            : useFullText
                ? "CASE WHEN p.ProductCode = @Keyword THEN 0 WHEN p.ProductCode LIKE @KeywordPrefix ESCAPE N'~' THEN 1 WHEN ft.[RANK] IS NOT NULL THEN 2 WHEN p.ProductName LIKE @KeywordPrefix ESCAPE N'~' THEN 3 ELSE 4 END ASC, ISNULL(ft.[RANK], 0) DESC,"
                : useNameContainsSearch
                    ? "CASE WHEN p.ProductCode = @Keyword THEN 0 WHEN p.ProductCode LIKE @KeywordPrefix ESCAPE N'~' THEN 1 WHEN p.ProductName LIKE @KeywordPrefix ESCAPE N'~' THEN 2 WHEN p.ProductName LIKE @KeywordContains ESCAPE N'~' THEN 3 ELSE 4 END ASC,"
                    : "CASE WHEN p.ProductCode = @Keyword THEN 0 WHEN p.ProductCode LIKE @KeywordPrefix ESCAPE N'~' THEN 1 WHEN p.ProductName LIKE @KeywordPrefix ESCAPE N'~' THEN 2 ELSE 3 END ASC,";

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
                p.StockQuantity,
                CASE WHEN p.StockQuantity > 0 THEN N'Còn hàng' ELSE N'Hết hàng' END AS StockStatus,
                p.Description,
                p.IsActive,
                p.CreatedByUserId,
                createdUser.UserName AS CreatedByUsername,
                p.LastModifiedByUserId,
                modifiedUser.UserName AS LastModifiedByUsername,
                p.IsAdminProtected,
                p.IsDeleted,
                p.DeletedAt,
                p.DeletedByUserId,
                deletedUser.UserName AS DeletedByUsername,
                p.CreatedAt,
                p.UpdatedAt
            FROM dbo.Products p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            LEFT JOIN dbo.AspNetUsers createdUser ON createdUser.Id = p.CreatedByUserId
            LEFT JOIN dbo.AspNetUsers modifiedUser ON modifiedUser.Id = p.LastModifiedByUserId
            LEFT JOIN dbo.AspNetUsers deletedUser ON deletedUser.Id = p.DeletedByUserId
            {fullTextJoin}
            WHERE {whereSql}
            ORDER BY {keywordOrder} {sortColumn} {sortDirection}, p.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.Products p
            INNER JOIN dbo.Categories c ON c.Id = p.CategoryId
            {fullTextJoin}
            WHERE {whereSql};
            """;

        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var grid = await connection.QueryMultipleAsync(command);
        var items = (await grid.ReadAsync<ProductResponse>()).AsList();
        var totalItems = await grid.ReadSingleAsync<int>();

        return PagedResult<ProductResponse>.Create(items, request.Page, request.PageSize, totalItems);
    }

    private static async Task<bool> IsProductNameFullTextEnabledAsync(
        System.Data.Common.DbConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT CAST(CASE
                WHEN ISNULL(FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'), 0) = 1
                 AND EXISTS (
                    SELECT 1
                    FROM sys.fulltext_indexes
                    WHERE object_id = OBJECT_ID(N'dbo.Products')
                 )
                THEN 1 ELSE 0 END AS bit);
            """;

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command);
    }

    private static string? BuildFullTextQuery(string keyword)
    {
        var terms = new List<string>();
        var current = new List<char>();

        foreach (var character in keyword)
        {
            if (char.IsLetterOrDigit(character))
            {
                current.Add(character);
                continue;
            }

            AddCurrentTerm();
        }

        AddCurrentTerm();
        return terms.Count == 0 ? null : string.Join(" AND ", terms);

        void AddCurrentTerm()
        {
            if (current.Count == 0) return;
            var term = new string(current.ToArray()).Replace("\"", "\"\"", StringComparison.Ordinal);
            terms.Add($"\"{term}*\"");
            current.Clear();
        }
    }

    private static string EscapeLikePattern(string value)
        => value
            .Replace("~", "~~", StringComparison.Ordinal)
            .Replace("%", "~%", StringComparison.Ordinal)
            .Replace("_", "~_", StringComparison.Ordinal)
            .Replace("[", "~[", StringComparison.Ordinal);

    private static bool RequiresLiteralNameSearch(string value)
        => value.Any(character => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character));
}
