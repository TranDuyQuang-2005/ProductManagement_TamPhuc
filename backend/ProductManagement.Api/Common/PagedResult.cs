namespace ProductManagement.Api.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages)
{
    public static PagedResult<T> Create(IEnumerable<T> items, int page, int pageSize, int totalItems)
    {
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);
        return new PagedResult<T>(items.ToList(), page, pageSize, totalItems, totalPages);
    }
}
