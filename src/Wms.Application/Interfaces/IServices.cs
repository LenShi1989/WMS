using Wms.Domain.Enums;

namespace Wms.Application.Interfaces;

/// <summary>目前登入使用者資訊。</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Username { get; }
    string? IpAddress { get; }
    bool IsAuthenticated { get; }
}

/// <summary>密碼雜湊。</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>JWT 簽發。</summary>
public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(
        Guid userId, string username, string displayName, IEnumerable<string> roles, IEnumerable<string> permissions);

    string CreateRefreshToken();
}

/// <summary>單號產生器，例如 IB20260927001。</summary>
public interface INumberGenerator
{
    Task<string> NextAsync(string prefix, CancellationToken ct = default);
}

/// <summary>操作紀錄寫入。</summary>
public interface IAuditService
{
    Task LogAsync(
        string action,
        string module,
        string? referenceType = null,
        Guid? referenceId = null,
        string? referenceNo = null,
        object? oldValue = null,
        object? newValue = null,
        CancellationToken ct = default);
}
