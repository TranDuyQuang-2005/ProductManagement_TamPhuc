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
            throw AppException.BadRequest("Trường sắp xếp không hợp lệ.", "sortBy", "Vui lòng chọn trường sắp xếp hợp lệ.");
        }

        if (!string.Equals(request.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            throw AppException.BadRequest("Chiều sắp xếp không hợp lệ.", "sortDirection", "Vui lòng chọn chiều sắp xếp hợp lệ.");
        }

        if (request.FromDate.HasValue && request.ToDate.HasValue && request.FromDate.Value.Date > request.ToDate.Value.Date)
        {
            throw AppException.BadRequest("Từ ngày không được lớn hơn đến ngày.", "fromDate", "Khoảng thời gian không hợp lệ.");
        }

        return await queryRepository.SearchAsync(request, cancellationToken);
    }

    public async Task<AuditLogResponse> GetByIdAsync(long id, CancellationToken cancellationToken)
        => await queryRepository.GetByIdAsync(id, cancellationToken)
           ?? throw AppException.NotFound($"Không tìm thấy nhật ký hoạt động có Id = {id}.");
}
