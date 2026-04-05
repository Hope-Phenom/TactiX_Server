namespace TactiX_Server.Services;

/// <summary>
/// 开发模式登录服务（仅用于开发和测试）
/// 无需真实OAuth，直接输入测试用户ID即可登录
/// </summary>
public class DevAuthService : IOAuthProvider
{
    public string ProviderName => "dev";

    private readonly ILogger<DevAuthService> _logger;

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
            "admin" => new OAuthLoginResult
            {
                Success = true,
                OAuthId = "dev_admin",
                Nickname = "测试管理员",
                AvatarUrl = "https://via.placeholder.com/100"
            },
            "pro" => new OAuthLoginResult
            {
                Success = true,
                OAuthId = "dev_pro",
                Nickname = "测试职业选手",
                AvatarUrl = "https://via.placeholder.com/100"
            },
            "verified" => new OAuthLoginResult
            {
                Success = true,
                OAuthId = "dev_verified",
                Nickname = "测试认证作者",
                AvatarUrl = "https://via.placeholder.com/100"
            },
            _ => new OAuthLoginResult
            {
                Success = true,
                OAuthId = $"dev_{devUserId}",
                Nickname = $"测试用户-{devUserId}",
                AvatarUrl = "https://via.placeholder.com/100"
            }
        };

        return Task.FromResult(result);
    }
}