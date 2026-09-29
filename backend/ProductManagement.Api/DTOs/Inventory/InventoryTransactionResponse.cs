namespace ProductManagement.Api.DTOs.Inventory;

public sealed record InventoryTransactionResponse(
    long Id,
    int ProductId,
    string MovementType,
    decimal QuantityChange,
    decimal QuantityBefore,
    decimal QuantityAfter,
    string? ReferenceCode,
    string? Note,
    string? CreatedByUserId,
    string? Username,
    DateTime CreatedAt);
