namespace TactiX_Server.Models.Config;

/// <summary>
/// QQ OAuth配置
/// </summary>
public class QQOAuthConfig
{
    /// <summary>QQ互联App ID</summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>QQ互联App Key</summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>回调地址</summary>
    public string CallbackUrl { get; set; } = string.Empty;
}