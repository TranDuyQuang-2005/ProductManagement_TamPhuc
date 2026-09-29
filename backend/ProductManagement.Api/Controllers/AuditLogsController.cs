using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.DTOs.Common;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Controllers;

[Authorize(Policy = AuthorizationPolicies.StaffOrAdmin)]
[ApiController]
[Route("api/audit-logs")]
public sealed class AuditLogsController(IAuditLogService service) : ControllerBase
{
    [HttpPost("search")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<AuditLogResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogResponse>>>> Search(
        [FromBody] AuditLogSearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.SearchAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<AuditLogResponse>>.Ok(result));
    }

    [HttpPost("get")]
    [ProducesResponseType(typeof(ApiResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AuditLogResponse>>> GetById(
        [FromBody] LongIdRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(request.Id, cancellationToken);
        return Ok(ApiResponse<AuditLogResponse>.Ok(result));
    }
}
