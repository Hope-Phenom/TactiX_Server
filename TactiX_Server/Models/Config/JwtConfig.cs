namespace TactiX_Server.Models.Config;

/// <summary>
/// JWT配置
/// </summary>
public class JwtConfig
{
    /// <summary>JWT密钥（至少32位）</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>发行者</summary>
    public string Issuer { get; set; } = "TactiX";

    /// <summary>受众</summary>
    public string Audience { get; set; } = "TactiX-Client";

    /// <summary>Access Token过期时间（小时）</summary>
    public int AccessTokenExpiryHours { get; set; } = 2;

    /// <summary>Refresh Token过期时间（天）</summary>
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
