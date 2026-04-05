using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TactiX_Server.Models.Config;

namespace TactiX_Server.Middleware;

/// <summary>
/// JWT认证扩展方法
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// 添加JWT认证服务
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var jwtConfig = jwtSection.Get<JwtConfig>();

        if (jwtConfig == null)
        {
            throw new InvalidOperationException("JWT configuration is missing. Please add 'Jwt' section to appsettings.json");
        }

        services.Configure<JwtConfig>(jwtSection);

        // 缓存SecurityKey避免重复计算
        var cachedSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = cachedSigningKey,
                ValidateIssuer = true,
                ValidIssuer = jwtConfig.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtConfig.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogWarning("JWT authentication failed: {Message}", context.Exception.Message);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                    logger.LogDebug("JWT token validated for user: {UserId}", userId);
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}