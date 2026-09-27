using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wms.Application.Interfaces;

namespace Wms.Infrastructure.Services;

/// <summary>JWT 設定，對應 appsettings 的 Jwt 區段。</summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "WmsApi";
    public string Audience { get; set; } = "WmsWeb";
    public string SecretKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
}

public class BCryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // 資料庫裡的雜湊格式損毀時，一律視為驗證失敗而不是丟例外。
            return false;
        }
    }
}

public class JwtTokenService(IOptions<JwtOptions> options) : IJwtTokenService
{
    /// <summary>權限寫進 permission claim，讓 Authorization Policy 直接比對，不必每次查資料庫。</summary>
    public const string PermissionClaimType = "permission";

    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAt) CreateAccessToken(
        Guid userId, string username, string displayName, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new("displayName", displayName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaimType, permission)));

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_options.SecretKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler();
        return (handler.CreateToken(descriptor), expiresAt);
    }

    public string CreateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
}
