using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.AuditLogs;

public sealed class AuditLogSearchRequest
{
    [Range(1, 1_000_000)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;

    [StringLength(450)]
    public string? UserId { get; init; }

    [StringLength(256)]
    public string? Username { get; init; }

    [StringLength(50)]
    public string? Action { get; init; }

    [StringLength(50)]
    public string? EntityType { get; init; }

    [StringLength(100)]
    public string? EntityId { get; init; }

    [StringLength(100)]
    public string? EntityCode { get; init; }

    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }

    [StringLength(50)]
    public string SortBy { get; init; } = "createdAt";

    [StringLength(4)]
    public string SortDirection { get; init; } = "desc";
}
