using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Categories;

public sealed class CategoryCreateRequest
{
    [Required(ErrorMessage = "Mã danh mục là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Mã danh mục tối đa 50 ký tự.")]
    public string CategoryCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên danh mục tối đa 200 ký tự.")]
    public string CategoryName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Tiền tố mã hàng là bắt buộc.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Tiền tố mã hàng phải từ 2 đến 10 ký tự.")]
    public string CodePrefix { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
    public string? Description { get; init; }

    public bool IsActive { get; init; } = true;
}
