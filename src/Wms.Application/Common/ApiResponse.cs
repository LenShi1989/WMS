namespace Wms.Application.Common;

/// <summary>API 統一回應格式。</summary>
public class ApiResponse
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public List<ApiError> Errors { get; set; } = [];

    public static ApiResponse Ok(string? message = null) => new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, params ApiError[] errors)
        => new() { Success = false, Message = message, Errors = [.. errors] };
}

/// <summary>帶資料的 API 統一回應格式。</summary>
public class ApiResponse<T> : ApiResponse
{
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static new ApiResponse<T> Fail(string message, params ApiError[] errors)
        => new() { Success = false, Message = message, Errors = [.. errors] };
}

/// <summary>單一錯誤項目。</summary>
public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public ApiError() { }

    public ApiError(string code, string message)
    {
        Code = code;
        Message = message;
    }
}
