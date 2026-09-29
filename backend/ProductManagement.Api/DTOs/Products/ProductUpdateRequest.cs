using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Products;

public sealed class ProductUpdateRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Id hang hoa khong hop le.")]
    public int Id { get; init; }

    [Required(ErrorMessage = "Ten hang hoa la bat buoc.")]
    [StringLength(250, ErrorMessage = "Ten hang hoa toi da 250 ky tu.")]
    public string ProductName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Don vi tinh la bat buoc.")]
    [StringLength(50, ErrorMessage = "Don vi tinh toi da 50 ky tu.")]
    public string Unit { get; init; } = string.Empty;

    [Required(ErrorMessage = "Gia ban la bat buoc.")]
    [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Gia ban nam ngoai pham vi cho phep.")]
    public decimal? Price { get; init; }

    [StringLength(1000, ErrorMessage = "Mo ta toi da 1000 ky tu.")]
    public string? Description { get; init; }

    [Required(ErrorMessage = "Trang thai la bat buoc.")]
    public bool? IsActive { get; init; }
}
