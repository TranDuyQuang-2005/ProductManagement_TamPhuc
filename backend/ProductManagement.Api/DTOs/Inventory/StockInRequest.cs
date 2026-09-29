using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Inventory;

public sealed class StockInRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Id hang hoa khong hop le.")]
    public int ProductId { get; init; }

    [Required(ErrorMessage = "So luong nhap la bat buoc.")]
    [Range(typeof(decimal), "0.01", "9999999999999999.99", ErrorMessage = "So luong nhap phai lon hon 0 va trong pham vi cho phep.")]
    public decimal? Quantity { get; init; }

    [StringLength(50, ErrorMessage = "Ma chung tu toi da 50 ky tu.")]
    public string? ReferenceCode { get; init; }

    [StringLength(500, ErrorMessage = "Ghi chu toi da 500 ky tu.")]
    public string? Note { get; init; }
}
