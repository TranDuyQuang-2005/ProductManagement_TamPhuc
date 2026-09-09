using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Products;

public sealed class ProductSearchRequest
{
    [StringLength(250)]
    public string? Keyword { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? MinPrice { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? MaxPrice { get; init; }

    public StockStatusFilter StockStatus { get; init; } = StockStatusFilter.All;
    public bool? IsActive { get; init; }

    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 10;

    [StringLength(50)]
    public string SortBy { get; init; } = "createdAt";

    [StringLength(4)]
    public string SortDirection { get; init; } = "desc";
}
