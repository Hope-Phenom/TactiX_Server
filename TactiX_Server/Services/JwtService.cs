using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TactiX_Server.Models.Config;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// JWT服务接口
/// </summary>
public interface IJwtService
{
    /// <summary>生成Access Token</summary>
    string GenerateAccessToken(TacticsUserModel user);

    /// <summary>生成Refresh Token</summary>
    string GenerateRefreshToken();

    /// <summary>验证Token并获取Claims</summary>
    ClaimsPrincipal? ValidateToken(string token);

    /// <summary>从Token获取用户ID</summary>
    long? GetUserIdFromToken(string token);

    /// <summary>创建Token验证参数</summary>
    TokenValidationParameters CreateTokenValidationParameters();
}

/// <summary>
/// JWT服务实现
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtConfig _config;
    private readonly SecurityKey _cachedSigningKey;
    private readonly ILogger<JwtService> _logger;

    public JwtService(IOptions<JwtConfig> config, ILogger<JwtService> logger)
    {
        _config = config.Value;
        _logger = logger;
        _cachedSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.Secret));
    }

    public string GenerateAccessToken(TacticsUserModel user)
    {
        var credentials = new SigningCredentials(_cachedSigningKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Nickname ?? user.OAuthId),
            new Claim("level", user.LevelCode),
            new Claim("oauth_provider", user.OAuthProvider),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config.Issuer,
            audience: _config.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_config.AccessTokenExpiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = CreateTokenValidationParameters();

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "JWT token validation failed");
            return null;
        }
    }

    public long? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal == null) return null;

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
        {
            return null;
        }

        return userId;
    }

    /// <summary>
    /// 创建Token验证参数（供JwtService和JwtAuthMiddleware复用）
    /// </summary>
    public TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _cachedSigningKey,
            ValidateIssuer = true,
            ValidIssuer = _config.Issuer,
            ValidateAudience = true,
            ValidAudience = _config.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }
}