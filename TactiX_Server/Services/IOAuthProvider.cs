using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// OAuth登录结果
/// </summary>
public class OAuthLoginResult
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>OAuth用户ID</summary>
    public string? OAuthId { get; set; }

    /// <summary>昵称</summary>
    public string? Nickname { get; set; }

    /// <summary>头像URL</summary>
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// OAuth提供者接口
/// </summary>
public interface IOAuthProvider
{
    /// <summary>提供者名称（如qq, wechat, dev）</summary>
    string ProviderName { get; }

    /// <summary>获取OAuth登录URL</summary>
    Task<string> GetLoginUrlAsync(string redirectUri, string state);

    /// <summary>处理OAuth回调，获取用户信息</summary>
    Task<OAuthLoginResult> HandleCallbackAsync(string code, string redirectUri);
}
