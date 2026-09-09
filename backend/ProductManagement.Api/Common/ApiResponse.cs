namespace ProductManagement.Api.Common;

public sealed record ApiResponse<T>(bool Success, string Message, T? Data)
{
    public static ApiResponse<T> Ok(T data, string message = "Thành công.") => new(true, message, data);
}
