using Microsoft.EntityFrameworkCore;
using Wms.Application.Common;
using Wms.Application.Dtos;
using Wms.Application.Interfaces;
using Wms.Domain.Entities;

namespace Wms.Application.Services;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task LogoutAsync(CancellationToken ct = default);
}

public class AuthService(
    IWmsDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ICurrentUser currentUser,
    IAuditService audit) : IAuthService
{
    private const int RefreshTokenDays = 7;

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var username = request.Username.Trim();

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AppException("帳號或密碼錯誤。", "INVALID_CREDENTIALS", 401);
        }

        if (!user.IsActive)
        {
            throw new AppException("此帳號已停用，請聯絡系統管理員。", "USER_INACTIVE", 403);
        }

        var response = await IssueTokenAsync(user, ct);
        await audit.LogAsync("LOGIN", "AUTH", "User", user.Id, user.Username, null, null, ct);

        return response;
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, ct);

        if (user is null || user.RefreshTokenExpiresAt is null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
        {
            throw new AppException("Refresh Token 無效或已過期，請重新登入。", "INVALID_REFRESH_TOKEN", 401);
        }

        if (!user.IsActive)
        {
            throw new AppException("此帳號已停用，請聯絡系統管理員。", "USER_INACTIVE", 403);
        }

        return await IssueTokenAsync(user, ct);
    }

    public async Task<UserProfileDto> GetProfileAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
        {
            throw new AppException("尚未登入。", "UNAUTHORIZED", 401);
        }

        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw AppException.NotFound("找不到使用者資料。");

        return BuildProfile(user);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct)
            ?? throw new AppException("尚未登入。", "UNAUTHORIZED", 401);

        if (!passwordHasher.Verify(request.OldPassword, user.PasswordHash))
        {
            throw new AppException("舊密碼不正確。", "INVALID_PASSWORD");
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("CHANGE_PASSWORD", "AUTH", "User", user.Id, user.Username, null, null, ct);
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
        if (user is null)
        {
            return;
        }

        user.RefreshToken = null;
        user.RefreshTokenExpiresAt = null;

        await db.SaveChangesAsync(ct);
        await audit.LogAsync("LOGOUT", "AUTH", "User", user.Id, user.Username, null, null, ct);
    }

    private async Task<LoginResponse> IssueTokenAsync(User user, CancellationToken ct)
    {
        var profile = BuildProfile(user);
        var (token, expiresAt) = jwtTokenService.CreateAccessToken(
            user.Id, user.Username, user.DisplayName, profile.Roles, profile.Permissions);

        user.RefreshToken = jwtTokenService.CreateRefreshToken();
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(RefreshTokenDays);
        user.LastLoginAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);

        return new LoginResponse
        {
            AccessToken = token,
            RefreshToken = user.RefreshToken,
            ExpiresIn = (int)(expiresAt - DateTime.UtcNow).TotalSeconds,
            User = profile
        };
    }

    private static UserProfileDto BuildProfile(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Email = user.Email,
        Roles = [.. user.UserRoles.Select(ur => ur.Role.Name).Distinct()],
        Permissions = [.. user.UserRoles
            .SelectMany(ur => ur.Role.RolePermissions)
            .Select(rp => rp.Permission.Code)
            .Distinct()
            .Order()]
    };
}
