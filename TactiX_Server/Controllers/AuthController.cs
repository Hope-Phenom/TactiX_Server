using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Tactics;
using TactiX_Server.Services;

namespace TactiX_Server.Controllers;

/// <summary>
/// 认证控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly TacticsDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IEnumerable<IOAuthProvider> _oauthProviders;
    private readonly IAdminService _adminService;

    public AuthController(
        ILogger<AuthController> logger,
        TacticsDbContext context,
        IJwtService jwtService,
        IEnumerable<IOAuthProvider> oauthProviders,
        IAdminService adminService)
    {
        _logger = logger;
        _context = context;
        _jwtService = jwtService;
        _oauthProviders = oauthProviders;
        _adminService = adminService;
    }

    /// <summary>
    /// 获取OAuth登录URL
    /// </summary>
    [HttpGet("Login/{provider}")]
    public async Task<IActionResult> GetLoginUrl(string provider, [FromQuery] string? redirectUri = null)
    {
        var oauthProvider = _oauthProviders.FirstOrDefault(p =>
            p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (oauthProvider == null)
        {
            return BadRequest(new { message = $"不支持的登录提供商: {provider}" });
        }

        var state = Guid.NewGuid().ToString("N");
        var loginUrl = await oauthProvider.GetLoginUrlAsync(redirectUri ?? "/", state);

        return Ok(new
        {
            provider,
            loginUrl,
            state
        });
    }

    /// <summary>
    /// OAuth回调处理
    /// </summary>
    [HttpGet("Callback/{provider}")]
    public async Task<IActionResult> HandleCallback(
        string provider,
        [FromQuery] string code,
        [FromQuery] string? state = null)
    {
        var oauthProvider = _oauthProviders.FirstOrDefault(p =>
            p.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));

        if (oauthProvider == null)
        {
            return BadRequest(new { message = $"不支持的登录提供商: {provider}" });
        }

        try
        {
            var result = await oauthProvider.HandleCallbackAsync(code, "/");

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage ?? "登录失败" });
            }

            // 查找或创建用户
            var user = await _context.TacticsUsers
                .FirstOrDefaultAsync(u => u.OAuthProvider == provider && u.OAuthId == result.OAuthId);

            string levelCode = "normal";

            if (user == null)
            {
                // 检查是否为超级管理员
                if (_adminService.IsSuperAdminByName(result.Nickname))
                {
                    levelCode = "admin";
                }

                user = new TacticsUserModel
                {
                    OAuthProvider = provider,
                    OAuthId = result.OAuthId!,
                    LevelCode = levelCode,
                    Nickname = result.Nickname,
                    AvatarUrl = result.AvatarUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.TacticsUsers.Add(user);
                await _context.SaveChangesAsync();

                // 如果是超级管理员，自动创建管理员记录
                if (levelCode == "admin")
                {
                    var admin = new TacticsAdminModel
                    {
                        UserId = user.Id,
                        Role = "super_admin",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TacticsAdmins.Add(admin);
                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("新用户注册: {Provider}, OAuthId={OAuthId}, UserId={UserId}",
                    provider, result.OAuthId, user.Id);
            }
            else
            {
                levelCode = user.LevelCode;

                // 更新用户信息
                if (!string.IsNullOrEmpty(result.Nickname) && user.Nickname != result.Nickname)
                {
                    user.Nickname = result.Nickname;
                }
                if (!string.IsNullOrEmpty(result.AvatarUrl) && user.AvatarUrl != result.AvatarUrl)
                {
                    user.AvatarUrl = result.AvatarUrl;
                }
                user.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            // 生成Token
            var accessToken = _jwtService.GenerateAccessToken(user, levelCode);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // TODO: 存储refresh token到数据库或Redis

            return Ok(new
            {
                message = "登录成功",
                userId = user.Id,
                nickname = user.Nickname,
                avatarUrl = user.AvatarUrl,
                levelCode = user.LevelCode,
                accessToken,
                refreshToken,
                expiresIn = 7200 // 2小时
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OAuth回调处理失败: Provider={Provider}", provider);
            return StatusCode(500, new { message = "登录处理失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 开发模式登录（仅用于测试）
    /// </summary>
    [HttpGet("DevLogin")]
    public async Task<IActionResult> DevLogin(
        [FromQuery] string devUserId = "test",
        [FromQuery] string? state = null)
    {
        // 开发模式登录，直接调用处理
        return await HandleCallback("dev", devUserId, state);
    }

    /// <summary>
    /// 刷新Token
    /// </summary>
    [HttpPost("RefreshToken")]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // TODO: 验证refresh token并从数据库获取用户信息
        // 暂时返回错误
        return BadRequest(new { message = "Refresh token功能暂未实现" });
    }

    /// <summary>
    /// 登出
    /// </summary>
    [HttpPost("Logout")]
    public IActionResult Logout()
    {
        // TODO: 使当前token失效（加入黑名单或从Redis删除）
        return Ok(new { message = "登出成功" });
    }

    /// <summary>
    /// 获取用户等级信息
    /// </summary>
    [HttpGet("LevelInfo")]
    public async Task<IActionResult> GetLevelInfo()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { message = "请先登录" });
        }

        var user = await _context.TacticsUsers
            .Include(u => u.LevelConfig)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "用户不存在" });
        }

        var levelConfig = user.LevelConfig ?? await _context.UserLevelConfigs
            .FirstOrDefaultAsync(l => l.LevelCode == "normal");

        return Ok(new
        {
            levelCode = user.LevelCode,
            levelName = levelConfig?.LevelName,
            description = levelConfig?.Description,
            badgeColor = levelConfig?.BadgeColor,
            maxFileSize = levelConfig?.MaxFileSize,
            maxUploadCount = levelConfig?.MaxUploadCount,
            dailyUploadLimit = levelConfig?.DailyUploadLimit,
            instantNotification = levelConfig?.InstantNotification
        });
    }

    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}

/// <summary>
/// 刷新Token请求
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
