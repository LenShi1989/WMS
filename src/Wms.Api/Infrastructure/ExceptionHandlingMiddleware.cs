using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Wms.Application.Common;

namespace Wms.Api.Infrastructure;

/// <summary>把所有未處理例外轉成統一的 API 錯誤格式。</summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            logger.LogWarning("業務例外 {Code}：{Message}", ex.Code, ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.Message, new ApiError(ex.Code, ex.Message));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(ex, "並行衝突。");
            await WriteAsync(context, StatusCodes.Status409Conflict,
                "資料已被其他人更新，請重新整理後再試一次。",
                new ApiError("CONCURRENCY_CONFLICT", "庫存或單據已被其他作業異動。"));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
        {
            var (message, code) = TranslatePostgresError(pg);
            logger.LogWarning(ex, "資料庫限制違反 {SqlState}。", pg.SqlState);
            await WriteAsync(context, StatusCodes.Status409Conflict, message, new ApiError(code, pg.MessageText));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "未預期的錯誤。");
            await WriteAsync(context, StatusCodes.Status500InternalServerError,
                "系統發生未預期的錯誤，請聯絡系統管理員。",
                new ApiError("INTERNAL_ERROR", ex.Message));
        }
    }

    private static (string Message, string Code) TranslatePostgresError(PostgresException pg) => pg.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation => ("資料重複，請確認輸入內容。", "DUPLICATE_KEY"),
        PostgresErrorCodes.ForeignKeyViolation => ("此資料仍被其他紀錄參照，無法刪除。", "FOREIGN_KEY_VIOLATION"),
        PostgresErrorCodes.CheckViolation when pg.ConstraintName?.Contains("inventories") == true
            => ("庫存數量不合法，可能是可用庫存不足或發生超賣。", "INSUFFICIENT_STOCK"),
        PostgresErrorCodes.CheckViolation => ("資料不符合資料庫限制條件。", "CHECK_VIOLATION"),
        _ => ("資料庫操作失敗。", "DATABASE_ERROR")
    };

    private static async Task WriteAsync(HttpContext context, int statusCode, string message, ApiError error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = ApiResponse.Fail(message, error);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
