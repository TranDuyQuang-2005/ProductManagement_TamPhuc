using Dapper;
using ProductManagement.Api.Common;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.Repositories.Interfaces;

namespace ProductManagement.Api.Repositories;

public sealed class AuditLogQueryRepository(DapperContext context) : IAuditLogQueryRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortColumns =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["createdAt"] = "CreatedAt",
            ["username"] = "Username",
            ["action"] = "Action",
            ["entityType"] = "EntityType",
            ["entityCode"] = "EntityCode"
        };

    public async Task<PagedResult<AuditLogResponse>> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken)
    {
        var where = new List<string> { "1 = 1" };
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            where.Add("UserId = @UserId");
            parameters.Add("UserId", request.UserId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Username))
        {
            where.Add("CHARINDEX(@Username, Username) > 0");
            parameters.Add("Username", request.Username.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Action))
        {
            where.Add("Action = @Action");
            parameters.Add("Action", request.Action.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(request.EntityType))
        {
            where.Add("EntityType = @EntityType");
            parameters.Add("EntityType", request.EntityType.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(request.EntityId))
        {
            where.Add("EntityId = @EntityId");
            parameters.Add("EntityId", request.EntityId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.EntityCode))
        {
            where.Add("EntityCode = @EntityCode");
            parameters.Add("EntityCode", request.EntityCode.Trim().ToUpperInvariant());
        }

        if (request.FromDate.HasValue)
        {
            where.Add("CreatedAt >= @FromDate");
            parameters.Add("FromDate", request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            where.Add("CreatedAt < @ToDate");
            parameters.Add("ToDate", request.ToDate.Value.Date.AddDays(1));
        }

        var sortColumn = SortColumns.TryGetValue(request.SortBy, out var mapped) ? mapped : SortColumns["createdAt"];
        var sortDirection = string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        parameters.Add("Offset", (request.Page - 1) * request.PageSize);
        parameters.Add("PageSize", request.PageSize);
        var whereSql = string.Join(" AND ", where);

        var sql = $"""
            SELECT Id, UserId, Username, Action, EntityType, EntityId, EntityCode,
                   OldValues, NewValues, ChangedFields, IpAddress, Description, CreatedAt
            FROM dbo.AuditLogs
            WHERE {whereSql}
            ORDER BY {sortColumn} {sortDirection}, Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT COUNT(1)
            FROM dbo.AuditLogs
            WHERE {whereSql};
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, parameters, cancellationToken: cancellationToken);
        using var grid = await connection.QueryMultipleAsync(command);
        var items = (await grid.ReadAsync<AuditLogResponse>()).AsList();
        var totalItems = await grid.ReadSingleAsync<int>();
        return PagedResult<AuditLogResponse>.Create(items, request.Page, request.PageSize, totalItems);
    }

    public async Task<AuditLogResponse?> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT Id, UserId, Username, Action, EntityType, EntityId, EntityCode,
                   OldValues, NewValues, ChangedFields, IpAddress, Description, CreatedAt
            FROM dbo.AuditLogs
            WHERE Id = @Id;
            """;

        await using var connection = context.CreateConnection();
        var command = new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AuditLogResponse>(command);
    }
}
