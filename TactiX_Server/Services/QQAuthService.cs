using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TactiX_Server.Models.Config;

namespace TactiX_Server.Services;

/// <summary>
/// QQ OAuth认证服务
/// </summary>
public class QQAuthService : IOAuthProvider
{
    public string ProviderName => OAuthProviders.QQ;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly QQOAuthConfig _config;
    private readonly ILogger<QQAuthService> _logger;

    private const string AuthorizeUrl = "https://graph.qq.com/oauth2.0/authorize";
    private const string TokenUrl = "https://graph.qq.com/oauth2.0/token";
    private const string OpenIdUrl = "https://graph.qq.com/oauth2.0/me";
    private const string UserInfoUrl = "https://graph.qq.com/user/get_user_info";

    public QQAuthService(IHttpClientFactory httpClientFactory, IOptions<QQOAuthConfig> config, ILogger<QQAuthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_config.AppId) || string.IsNullOrEmpty(_config.AppKey))
        {
            throw new InvalidOperationException("QQ OAuth配置缺失: TACTIX_QQ_APP_ID 或 TACTIX_QQ_APP_KEY 未设置");
        }
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient("QQAuth");

    public Task<string> GetLoginUrlAsync(string redirectUri, string state)
    {
        var callbackUrl = Uri.EscapeDataString(_config.CallbackUrl);
        var url = $"{AuthorizeUrl}?response_type=code&client_id={_config.AppId}&redirect_uri={callbackUrl}&state={state}&scope=get_user_info";
        return Task.FromResult(url);
    }

    public async Task<OAuthLoginResult> HandleCallbackAsync(string code, string redirectUri)
    {
        try
        {
            // Step 1: 获取AccessToken
            var tokenResponse = await GetAccessTokenAsync(code);
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return new OAuthLoginResult
                {
                    Success = false,
                    ErrorMessage = "获取AccessToken失败"
                };
            }

            // Step 2: 获取OpenID
            var openId = await GetOpenIdAsync(tokenResponse.AccessToken);
            if (string.IsNullOrEmpty(openId))
            {
                return new OAuthLoginResult
                {
                    Success = false,
                    ErrorMessage = "获取OpenID失败"
                };
            }

            // Step 3: 获取用户信息
            var userInfo = await GetUserInfoAsync(tokenResponse.AccessToken, openId);

            _logger.LogInformation("QQ OAuth登录成功: OpenId={OpenId}, Nickname={Nickname}", openId, userInfo?.Nickname);

            return new OAuthLoginResult
            {
                Success = true,
                OAuthId = openId,
                Nickname = userInfo?.Nickname,
                AvatarUrl = userInfo?.AvatarUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QQ OAuth回调处理失败");
            return new OAuthLoginResult
            {
                Success = false,
                ErrorMessage = "登录处理失败: " + ex.Message
            };
        }
    }

    /// <summary>
    /// 获取AccessToken
    /// QQ使用GET请求，响应格式为URL编码字符串
    /// </summary>
    private async Task<QQTokenResponse?> GetAccessTokenAsync(string code)
    {
        var callbackUrl = Uri.EscapeDataString(_config.CallbackUrl);
        var url = $"{TokenUrl}?grant_type=authorization_code&client_id={_config.AppId}&client_secret={_config.AppKey}&code={code}&redirect_uri={callbackUrl}";

        var response = await GetClient().GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("QQ Token请求失败: {StatusCode}, {Content}", response.StatusCode, content);
            return null;
        }

        // 解析URL编码响应: access_token=xxx&expires_in=7776000&refresh_token=yyy
        return ParseTokenResponse(content);
    }

    /// <summary>
    /// 解析Token响应（URL编码格式）
    /// </summary>
    private QQTokenResponse? ParseTokenResponse(string content)
    {
        try
        {
            // 格式: access_token=xxx&expires_in=7776000&refresh_token=yyy
            var parts = content.Split('&');
            var dict = new Dictionary<string, string>();

            foreach (var part in parts)
            {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length >= 2 && !string.IsNullOrEmpty(keyValue[0]))
                {
                    dict[keyValue[0]] = Uri.UnescapeDataString(keyValue[1]);
                }
            }

            if (!dict.TryGetValue("access_token", out var accessToken))
            {
                _logger.LogWarning("Token响应缺少access_token: {Content}", content);
                return null;
            }

            return new QQTokenResponse
            {
                AccessToken = accessToken,
                ExpiresIn = dict.TryGetValue("expires_in", out var expiresIn) ? int.Parse(expiresIn) : 0,
                RefreshToken = dict.TryGetValue("refresh_token", out var refreshToken) ? refreshToken : string.Empty
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析Token响应失败: {Content}", content);
            return null;
        }
    }

    /// <summary>
    /// 获取OpenID
    /// QQ响应格式为JSONP: callback( {"client_id":"xxx","openid":"yyy"} );
    /// </summary>
    private async Task<string?> GetOpenIdAsync(string accessToken)
    {
        var url = $"{OpenIdUrl}?access_token={accessToken}";
        var response = await GetClient().GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("QQ OpenId请求失败: {StatusCode}, {Content}", response.StatusCode, content);
            return null;
        }

        return ParseOpenIdResponse(content);
    }

    /// <summary>
    /// 解析OpenID响应（JSONP格式）
    /// </summary>
    private string? ParseOpenIdResponse(string content)
    {
        try
        {
            // 格式: callback( {"client_id":"xxx","openid":"yyy"} );
            // 或: {"client_id":"xxx","openid":"yyy"} (无callback包装)

            string json;
            if (content.StartsWith("callback") || content.StartsWith("Callback"))
            {
                var jsonStart = content.IndexOf('(') + 1;
                var jsonEnd = content.LastIndexOf(')');
                if (jsonStart < 1 || jsonEnd < 0 || jsonEnd <= jsonStart)
                {
                    _logger.LogWarning("OpenID响应格式错误: {Content}", content);
                    return null;
                }
                json = content.Substring(jsonStart, jsonEnd - jsonStart).Trim();
            }
            else
            {
                json = content;
            }

            var result = JsonSerializer.Deserialize<QQOpenIdResponse>(json);
            return result?.OpenId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析OpenID响应失败: {Content}", content);
            return null;
        }
    }

    /// <summary>
    /// 获取用户信息
    /// 注意: QQ使用oauth_consumer_key参数名，不是client_id
    /// </summary>
    private async Task<QQUserInfoResponse?> GetUserInfoAsync(string accessToken, string openId)
    {
        var url = $"{UserInfoUrl}?access_token={accessToken}&oauth_consumer_key={_config.AppId}&openid={openId}";
        var response = await GetClient().GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("QQ UserInfo请求失败: {StatusCode}, {Content}", response.StatusCode, content);
            return null;
        }

        try
        {
            var result = JsonSerializer.Deserialize<QQUserInfoResponse>(content);

            // 检查返回状态
            if (result?.Ret != 0)
            {
                _logger.LogWarning("QQ UserInfo返回错误: Ret={Ret}, Msg={Msg}", result?.Ret, result?.Msg);
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析UserInfo响应失败: {Content}", content);
            return null;
        }
    }
}

/// <summary>
/// QQ Token响应
/// </summary>
internal class QQTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// QQ OpenID响应
/// </summary>
internal class QQOpenIdResponse
{
    public string ClientId { get; set; } = string.Empty;
    public string OpenId { get; set; } = string.Empty;
}

/// <summary>
/// QQ用户信息响应
/// </summary>
internal class QQUserInfoResponse
{
    public int Ret { get; set; }
    public string Msg { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;

    /// <summary>小头像(30x30)</summary>
    public string FigureUrl { get; set; } = string.Empty;

    /// <summary>中头像(50x50)</summary>
    public string FigureUrl1 { get; set; } = string.Empty;

    /// <summary>大头像(100x100)</summary>
    public string FigureUrl2 { get; set; } = string.Empty;

    /// <summary>用户头像URL（使用大头像）</summary>
    public string AvatarUrl => FigureUrl2;
}