using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.AuditLogs;

namespace ProductManagement.Api.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogResponse>> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken);
    Task<AuditLogResponse> GetByIdAsync(long id, CancellationToken cancellationToken);
}
