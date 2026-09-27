using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;

namespace Wms.Api.Controllers;

/// <summary>所有 API 的共同基底：統一路由前綴、統一回應包裝、預設需要登入。</summary>
[ApiController]
[Authorize]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected static ApiResponse<T> Success<T>(T data, string? message = null) => ApiResponse<T>.Ok(data, message);

    protected static ApiResponse Success(string? message = null) => ApiResponse.Ok(message);
}
