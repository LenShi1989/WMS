using System.Security.Claims;
using Wms.Application.Interfaces;

namespace Wms.Api.Infrastructure;

/// <summary>從 HttpContext 的 JWT Claims 取出目前登入者資訊。</summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var value = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Username => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
