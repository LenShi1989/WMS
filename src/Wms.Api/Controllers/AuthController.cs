using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Services;

namespace Wms.Api.Controllers;

/// <summary>登入、換發 Token 與個人資料。</summary>
[Route("api/v1/auth")]
public class AuthController(IAuthService authService) : ApiControllerBase
{
    /// <summary>使用帳號密碼登入，取得 JWT。</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResponse<LoginResponse>> Login(LoginRequest request, CancellationToken ct)
        => Success(await authService.LoginAsync(request, ct));

    /// <summary>以 Refresh Token 換發新的 Access Token。</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ApiResponse<LoginResponse>> Refresh(RefreshTokenRequest request, CancellationToken ct)
        => Success(await authService.RefreshAsync(request, ct));

    /// <summary>取得目前登入者的資料與權限清單。</summary>
    [HttpGet("profile")]
    public async Task<ApiResponse<UserProfileDto>> Profile(CancellationToken ct)
        => Success(await authService.GetProfileAsync(ct));

    /// <summary>變更自己的密碼。</summary>
    [HttpPost("change-password")]
    public async Task<ApiResponse> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(request, ct);
        return Success("密碼已更新，請重新登入。");
    }

    /// <summary>登出並使 Refresh Token 失效。</summary>
    [HttpPost("logout")]
    public async Task<ApiResponse> Logout(CancellationToken ct)
    {
        await authService.LogoutAsync(ct);
        return Success("已登出。");
    }
}
