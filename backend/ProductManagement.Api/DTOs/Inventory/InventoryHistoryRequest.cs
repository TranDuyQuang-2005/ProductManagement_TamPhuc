using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Inventory;

public sealed class InventoryHistoryRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Id hang hoa khong hop le.")]
    public int ProductId { get; init; }

    [Range(1, 1_000_000, ErrorMessage = "Trang khong hop le.")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "So ban ghi moi trang khong hop le.")]
    public int PageSize { get; init; } = 10;
}
