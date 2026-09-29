using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.Categories;
using ProductManagement.Api.DTOs.Common;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthorizationPolicies.StaffOrAdmin)]
[Route("api/[controller]")]
public sealed class CategoriesController(ICategoryService service) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryResponse>>>> Search(
        [FromBody] CategorySearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<CategoryResponse>>.Ok(result));
    }

    [HttpPost("options")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryOptionResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryOptionResponse>>>> GetOptions(
        [FromBody] CategoryOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetOptionsAsync(request.ActiveOnly, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<CategoryOptionResponse>>.Ok(result));
    }

    [HttpPost("get")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetById(
        [FromBody] IdRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(request.Id, cancellationToken);
        return Ok(ApiResponse<CategoryResponse>.Ok(result));
    }

    [HttpPost("product-code-preview")]
    [ProducesResponseType(typeof(ApiResponse<ProductCodePreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductCodePreviewResponse>>> GetProductCodePreview(
        [FromBody] ProductCodePreviewRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetProductCodePreviewAsync(request.Id, cancellationToken);
        return Ok(ApiResponse<ProductCodePreviewResponse>.Ok(result));
    }

    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Create(
        [FromBody] CategoryCreateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<CategoryResponse>(true, "Them danh muc thanh cong.", result));
    }

    [HttpPost("update")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> Update(
        [FromBody] CategoryUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(request.Id, request, cancellationToken);
        return Ok(new ApiResponse<CategoryResponse>(true, "Cap nhat danh muc thanh cong.", result));
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
        return Ok(new ApiResponse<object>(true, "Da xoa danh muc.", null));
    }
}
