using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.DTOs.Common;
using ProductManagement.Api.DTOs.Products;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.StaffOrAdmin)]
[Route("api/[controller]")]
public sealed class ProductsController(IProductService service) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductResponse>>>> Search(
        [FromBody] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductResponse>>.Ok(result));
    }

    [HttpPost("trash/search")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductResponse>>>> SearchTrash(
        [FromBody] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchTrashAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductResponse>>.Ok(result));
    }

    [HttpPost("get")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> GetById(
        [FromBody] IdRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(request.Id, cancellationToken);
        return Ok(ApiResponse<ProductResponse>.Ok(result));
    }

    [HttpPost("history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogResponse>>>> GetHistory(
        [FromBody] ProductHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetHistoryAsync(request.Id, request.Page, request.PageSize, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLogResponse>>.Ok(result));
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Create(
        [FromBody] ProductCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<ProductResponse>(true, "Them hang hoa thanh cong.", result));
    }

    [HttpPost("update")]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Update(
        [FromBody] ProductUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(request.Id, request, cancellationToken);
        return Ok(new ApiResponse<ProductResponse>(true, "Cap nhat hang hoa thanh cong.", result));
    }

    [HttpPost("delete")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        [FromBody] IdRequest request,
        CancellationToken cancellationToken)
    {
        await service.DeleteAsync(request.Id, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Da xoa hang hoa.", null));
    }

    [HttpPost("restore")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ProductResponse>>> Restore(
        [FromBody] IdRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.RestoreAsync(request.Id, cancellationToken);
        return Ok(new ApiResponse<ProductResponse>(true, "Khoi phuc hang hoa thanh cong.", result));
    }

    [HttpPost("delete-permanent")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object>>> DeletePermanent(
        [FromBody] IdRequest request,
        CancellationToken cancellationToken)
    {
        await service.DeletePermanentAsync(request.Id, cancellationToken);
        return Ok(new ApiResponse<object>(true, "Xoa vinh vien hang hoa thanh cong.", null));
    }
}
