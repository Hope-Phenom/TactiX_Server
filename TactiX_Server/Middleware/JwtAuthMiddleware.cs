using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        var jwtConfig = configuration.GetSection("Jwt").Get<JwtConfig>() ?? new JwtConfig();

        services.Configure<JwtConfig>(configuration.GetSection("Jwt"));

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
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.Secret)),
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
                    Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine($"Token validated for: {context.Principal?.Identity?.Name}");
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }
}
