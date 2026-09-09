using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProductManagement.Api.Common;

namespace ProductManagement.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            logger.LogInformation("Request was cancelled by the client. TraceId: {TraceId}", context.TraceIdentifier);
        }
        catch (AppException ex)
        {
            logger.LogWarning(ex, "Business error {StatusCode}. TraceId: {TraceId}", ex.StatusCode, context.TraceIdentifier);
            await WriteErrorAsync(context, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (DbUpdateException ex) when (TryGetSqlException(ex, out var sqlException))
        {
            logger.LogWarning(ex, "Database update error {SqlNumber}. TraceId: {TraceId}", sqlException.Number, context.TraceIdentifier);

            if (sqlException.Number is 2601 or 2627)
            {
                var (message, errors) = GetDuplicateError(sqlException);
                await WriteErrorAsync(context, (int)HttpStatusCode.Conflict, message, errors);
                return;
            }

            if (sqlException.Number == 547)
            {
                await WriteErrorAsync(context, (int)HttpStatusCode.Conflict,
                    "Không thể thực hiện thao tác vì dữ liệu đang được tham chiếu bởi bản ghi khác.");
                return;
            }

            await WriteErrorAsync(context, (int)HttpStatusCode.InternalServerError,
                "Có lỗi khi cập nhật dữ liệu trong cơ sở dữ liệu.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteErrorAsync(context, (int)HttpStatusCode.InternalServerError,
                "Hệ thống gặp lỗi không mong muốn. Vui lòng thử lại.");
        }
    }

    private static (string Message, IDictionary<string, string[]>? Errors) GetDuplicateError(SqlException exception)
    {
        if (exception.Message.Contains("UX_Products_ProductCode", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "Mã hàng hóa đã tồn tại.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["productCode"] = ["Mã hàng hóa đã tồn tại."]
                });
        }

        if (exception.Message.Contains("UX_Categories_CategoryCode", StringComparison.OrdinalIgnoreCase))
        {
            return (
                "Mã danh mục đã tồn tại.",
                new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
                {
                    ["categoryCode"] = ["Mã danh mục đã tồn tại."]
                });
        }

        return ("Dữ liệu bị trùng với bản ghi đã tồn tại. Vui lòng kiểm tra lại mã.", null);
    }

    private static bool TryGetSqlException(DbUpdateException exception, out SqlException sqlException)
    {
        Exception? current = exception;
        while (current is not null)
        {
            if (current is SqlException sql)
            {
                sqlException = sql;
                return true;
            }
            current = current.InnerException;
        }

        sqlException = null!;
        return false;
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message,
        IDictionary<string, string[]>? errors = null)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
            false,
            message,
            errors,
            context.TraceIdentifier));
    }
}
