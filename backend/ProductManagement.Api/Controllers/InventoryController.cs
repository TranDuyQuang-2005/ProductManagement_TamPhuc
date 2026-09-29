using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.Inventory;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.StaffOrAdmin)]
[Route("api/[controller]")]
public sealed class InventoryController(IInventoryService service) : ControllerBase
{
    [HttpPost("stock-in")]
    [ProducesResponseType(typeof(ApiResponse<InventoryTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<InventoryTransactionResponse>>> StockIn(
        [FromBody] StockInRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.StockInAsync(request, cancellationToken);
        return Ok(new ApiResponse<InventoryTransactionResponse>(true, "Nhap hang thanh cong.", result));
    }

    [HttpPost("history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InventoryTransactionResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<InventoryTransactionResponse>>>> History(
        [FromBody] InventoryHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetHistoryAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<InventoryTransactionResponse>>.Ok(result));
    }
}
