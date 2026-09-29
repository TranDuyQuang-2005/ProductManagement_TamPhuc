using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Categories;

public sealed class ProductCodePreviewRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Id danh muc khong hop le.")]
    public int Id { get; init; }
}
