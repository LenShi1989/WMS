using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Wms.Infrastructure.Services;

namespace Wms.Api.Infrastructure;

/// <summary>要求使用者擁有指定權限碼。</summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var granted = context.User.Claims
            .Any(c => c.Type == JwtTokenService.PermissionClaimType && c.Value == requirement.Permission);

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// 依 Policy 名稱動態產生權限 Policy，
/// 這樣 Controller 上寫 [Authorize(Policy = Permissions.MaterialView)] 就不必逐一註冊。
/// </summary>
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var existing = await base.GetPolicyAsync(policyName);
        if (existing is not null)
        {
            return existing;
        }

        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName))
            .Build();
    }
}
