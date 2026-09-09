namespace ProductManagement.Api.Common;

public sealed class AppException : Exception
{
    public int StatusCode { get; }
    public IDictionary<string, string[]>? Errors { get; }

    public AppException(int statusCode, string message, IDictionary<string, string[]>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public static AppException BadRequest(string message, string? field = null, string? fieldMessage = null)
        => new(400, message, BuildErrors(field, fieldMessage));

    public static AppException NotFound(string message) => new(404, message);

    public static AppException Forbidden(string message) => new(403, message);

    public static AppException Conflict(string message, string? field = null, string? fieldMessage = null)
        => new(409, message, BuildErrors(field, fieldMessage));

    private static IDictionary<string, string[]>? BuildErrors(string? field, string? fieldMessage)
    {
        if (string.IsNullOrWhiteSpace(field) || string.IsNullOrWhiteSpace(fieldMessage))
            return null;

        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [field] = [fieldMessage]
        };
    }
}
