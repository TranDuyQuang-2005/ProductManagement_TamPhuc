using System.Text.Json;
using ProductManagement.Api.Data;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Services;

public sealed class AuditService(
    AppDbContext dbContext,
    ICurrentUserService currentUser) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task AddAsync(
        string action,
        string entityType,
        string? entityId,
        string? entityCode,
        object? oldValues,
        object? newValues,
        IEnumerable<string>? changedFields,
        string? description,
        CancellationToken cancellationToken)
    {
        await dbContext.AuditLogs.AddAsync(new AuditLog
        {
            UserId = currentUser.UserId,
            Username = currentUser.Username,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityCode = entityCode,
            OldValues = ToJson(oldValues),
            NewValues = ToJson(newValues),
            ChangedFields = ToJson(changedFields?.ToArray()),
            IpAddress = currentUser.IpAddress,
            Description = description,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task AddAuthAsync(
        string action,
        ApplicationUser? user,
        string username,
        string? description,
        CancellationToken cancellationToken)
    {
        await dbContext.AuditLogs.AddAsync(new AuditLog
        {
            UserId = user?.Id,
            Username = user?.UserName ?? username.Trim(),
            Action = action,
            EntityType = "AUTH",
            EntityId = user?.Id,
            EntityCode = user?.UserName ?? username.Trim(),
            IpAddress = currentUser.IpAddress,
            Description = description,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private static string? ToJson(object? value)
        => value is null ? null : JsonSerializer.Serialize(value, JsonOptions);
}
