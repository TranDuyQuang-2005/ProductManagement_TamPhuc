using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.AuditLogs;

public sealed class AuditLogSearchRequest
{
    [Range(1, 1_000_000, ErrorMessage = "Trang không hợp lệ.")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "Số bản ghi mỗi trang không hợp lệ.")]
    public int PageSize { get; init; } = 20;

    [StringLength(450, ErrorMessage = "Mã người dùng tối đa 450 ký tự.")]
    public string? UserId { get; init; }

    [StringLength(256, ErrorMessage = "Tên đăng nhập tối đa 256 ký tự.")]
    public string? Username { get; init; }

    [StringLength(50, ErrorMessage = "Hành động tối đa 50 ký tự.")]
    public string? Action { get; init; }

    [StringLength(50, ErrorMessage = "Loại dữ liệu tối đa 50 ký tự.")]
    public string? EntityType { get; init; }

    [StringLength(100, ErrorMessage = "Mã bản ghi tối đa 100 ký tự.")]
    public string? EntityId { get; init; }

    [StringLength(100, ErrorMessage = "Mã hiển thị tối đa 100 ký tự.")]
    public string? EntityCode { get; init; }

    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }

    [StringLength(50, ErrorMessage = "Trường sắp xếp không hợp lệ.")]
    public string SortBy { get; init; } = "createdAt";

    [StringLength(4, ErrorMessage = "Chiều sắp xếp không hợp lệ.")]
    public string SortDirection { get; init; } = "desc";
}
