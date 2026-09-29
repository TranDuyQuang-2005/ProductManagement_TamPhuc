using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.Inventory;

namespace ProductManagement.Api.Services.Interfaces;

public interface IInventoryService
{
    Task<InventoryTransactionResponse> StockInAsync(StockInRequest request, CancellationToken cancellationToken);
    Task<PagedResult<InventoryTransactionResponse>> GetHistoryAsync(InventoryHistoryRequest request, CancellationToken cancellationToken);
}
