namespace Wms.Application.Common;

/// <summary>業務例外，會被 Middleware 轉成統一錯誤格式。</summary>
public class AppException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    public AppException(string message, string code = "BUSINESS_ERROR", int statusCode = 400)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }

    public static AppException NotFound(string message) => new(message, "NOT_FOUND", 404);

    public static AppException Conflict(string message, string code = "CONFLICT") => new(message, code, 409);

    public static AppException Forbidden(string message) => new(message, "FORBIDDEN", 403);

    public static AppException InsufficientStock(string message)
        => new(message, "INSUFFICIENT_STOCK", 409);
}
