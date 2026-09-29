namespace ProductManagement.Api.Entities;

public sealed class InventoryTransaction
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal QuantityChange { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Note { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
}
