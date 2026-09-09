namespace ProductManagement.Api.DTOs.AuditLogs;

public sealed record AuditLogResponse(
    long Id,
    string? UserId,
    string Username,
    string Action,
    string EntityType,
    string? EntityId,
    string? EntityCode,
    string? OldValues,
    string? NewValues,
    string? ChangedFields,
    string? IpAddress,
    string? Description,
    DateTime CreatedAt);
