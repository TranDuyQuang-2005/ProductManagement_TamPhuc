using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Products;

public sealed class ProductSearchRequest
{
    [StringLength(250, ErrorMessage = "Từ khóa tìm kiếm tối đa 250 ký tự.")]
    public string? Keyword { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Danh mục không hợp lệ.")]
    public int? CategoryId { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá thấp nhất không hợp lệ.")]
    public decimal? MinPrice { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Giá cao nhất không hợp lệ.")]
    public decimal? MaxPrice { get; init; }

    public StockStatusFilter StockStatus { get; init; } = StockStatusFilter.All;
    public bool? IsActive { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "Trang không hợp lệ.")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "Số bản ghi mỗi trang không hợp lệ.")]
    public int PageSize { get; init; } = 10;

    [StringLength(50, ErrorMessage = "Trường sắp xếp không hợp lệ.")]
    public string SortBy { get; init; } = "createdAt";

    [StringLength(4, ErrorMessage = "Chiều sắp xếp không hợp lệ.")]
    public string SortDirection { get; init; } = "desc";
}
