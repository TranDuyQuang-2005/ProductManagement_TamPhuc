namespace ProductManagement.Api.Common;

public sealed record ApiErrorResponse(
    bool Success,
    string Message,
    IDictionary<string, string[]>? Errors = null,
    string? TraceId = null);
