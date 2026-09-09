using ProductManagement.Api.Entities;

namespace ProductManagement.Api.Services.Interfaces;

public interface IAuditService
{
    Task AddAsync(
        string action,
        string entityType,
        string? entityId,
        string? entityCode,
        object? oldValues,
        object? newValues,
        IEnumerable<string>? changedFields,
        string? description,
        CancellationToken cancellationToken);

    Task AddAuthAsync(
        string action,
        ApplicationUser? user,
        string username,
        string? description,
        CancellationToken cancellationToken);
}
