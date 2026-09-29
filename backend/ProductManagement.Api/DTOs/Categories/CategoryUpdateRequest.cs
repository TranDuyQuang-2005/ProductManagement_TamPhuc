using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Categories;

public sealed class CategoryUpdateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Id danh muc khong hop le.")]
    public int Id { get; init; }

    [Required(ErrorMessage = "Ma danh muc la bat buoc.")]
    [StringLength(20, ErrorMessage = "Ma danh muc toi da 20 ky tu.")]
    public string CategoryCode { get; init; } = string.Empty;

    [Required(ErrorMessage = "Ten danh muc la bat buoc.")]
    [StringLength(200, ErrorMessage = "Ten danh muc toi da 200 ky tu.")]
    public string CategoryName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Tien to ma hang la bat buoc.")]
    [StringLength(10, MinimumLength = 2, ErrorMessage = "Tien to ma hang phai tu 2 den 10 ky tu.")]
    public string CodePrefix { get; init; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mo ta toi da 500 ky tu.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "Trang thai la bat buoc.")]
    public bool? IsActive { get; init; }
}
