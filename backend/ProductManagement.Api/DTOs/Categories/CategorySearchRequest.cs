using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Categories;

public sealed class CategorySearchRequest
{
    [StringLength(200, ErrorMessage = "Từ khóa tìm kiếm tối đa 200 ký tự.")]
    public string? Keyword { get; init; }

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
