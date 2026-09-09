using ProductManagement.Api.Common;
using ProductManagement.Api.DTOs.AuditLogs;
using ProductManagement.Api.Repositories.Interfaces;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Services;

public sealed class AuditLogService(IAuditLogQueryRepository queryRepository) : IAuditLogService
{
    private static readonly HashSet<string> AllowedSortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "createdAt", "username", "action", "entityType", "entityCode"
    };

    public async Task<PagedResult<AuditLogResponse>> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken)
    {
        if (!AllowedSortFields.Contains(request.SortBy))
        {
            throw AppException.BadRequest("Truong sap xep khong hop le.", "sortBy", $"Chi ho tro: {string.Join(", ", AllowedSortFields)}.");
        }

        if (!string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest("Chieu sap xep khong hop le.", "sortDirection", "Chi chap nhan 'asc' hoac 'desc'.");
        }

        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value.Date > request.ToDate.Value.Date)
        {
            throw AppException.BadRequest("Tu ngay khong duoc lon hon den ngay.", "fromDate", "Khoang thoi gian khong hop le.");
        }

        return await queryRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<AuditLogResponse> GetByIdAsync(long id, CancellationToken cancellationToken)
        => await queryRepository.GetByIdAsync(id, cancellationToken)
           ?? throw AppException.NotFound($"Khong tim thay audit log co Id = {id}.");
}
