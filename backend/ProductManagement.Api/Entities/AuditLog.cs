namespace ProductManagement.Api.Entities;

public sealed class AuditLog
{
    public long Id { get; set; }
    public string? UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? EntityCode { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedFields { get; set; }
    public string? IpAddress { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}
