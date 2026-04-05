namespace TactiX_Server.Services;

/// <summary>
/// OAuth提供者常量
/// </summary>
public static class OAuthProviders
{
    public const string Dev = "dev";
    public const string QQ = "qq";
    public const string WeChat = "wechat";
}

/// <summary>
/// 开发模式登录服务（仅用于开发和测试）
/// 无需真实OAuth，直接输入测试用户ID即可登录
/// </summary>
public class DevAuthService : IOAuthProvider
{
    public string ProviderName => OAuthProviders.Dev;

    private readonly ILogger<DevAuthService> _logger;

    private const string DefaultAvatarUrl = "https://via.placeholder.com/100";

    public DevAuthService(ILogger<DevAuthService> logger)
    {
        _logger = logger;
    }

    public Task<string> GetLoginUrlAsync(string redirectUri, string state)
    {
        // 开发模式下直接返回一个模拟的URL
        var url = $"/api/Auth/DevLogin?state={state}&redirect_uri={Uri.EscapeDataString(redirectUri)}";
        return Task.FromResult(url);
    }

    public Task<OAuthLoginResult> HandleCallbackAsync(string code, string redirectUri)
    {
        // code参数在开发模式下作为dev_user_id使用
        var devUserId = code;

        _logger.LogInformation("开发模式登录: DevUserId={DevUserId}", devUserId);

        // 内置测试用户
        var result = devUserId switch
        {
            UserLevels.Admin => new OAuthLoginResult
            {
                Success = true,
                OAuthId = "dev_admin",
                Nickname = "测试管理员",
                AvatarUrl = DefaultAvatarUrl
            },
            UserLevels.Pro => new OAuthLoginResult
            {
                Success = true,
                OAuthId = "dev_pro",
                Nickname = "测试职业选手",
                AvatarUrl = DefaultAvatarUrl
            },
            UserLevels.Verified => new OAuthLoginResult
            {
                Success = true,
                OAuthId = "dev_verified",
                Nickname = "测试认证作者",
                AvatarUrl = DefaultAvatarUrl
            },
            _ => new OAuthLoginResult
            {
                Success = true,
                OAuthId = $"dev_{devUserId}",
                Nickname = $"测试用户-{devUserId}",
                AvatarUrl = DefaultAvatarUrl
            }
        };

        return Task.FromResult(result);
    }
}