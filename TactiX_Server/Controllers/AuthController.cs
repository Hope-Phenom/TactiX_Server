using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TactiX_Server.Data;
using TactiX_Server.Models.Config;
using TactiX_Server.Models.Tactics;
using TactiX_Server.Services;
using TactiX_Server.Utils;

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
    private readonly JwtConfig _jwtConfig;

    public AuthController(
        ILogger<AuthController> logger,
        TacticsDbContext context,
        IJwtService jwtService,
        IEnumerable<IOAuthProvider> oauthProviders,
        IAdminService adminService,
        IOptions<JwtConfig> jwtConfig)
    {
        _logger = logger;
        _context = context;
        _jwtService = jwtService;
        _oauthProviders = oauthProviders;
        _adminService = adminService;
        _jwtConfig = jwtConfig.Value;
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

            string levelCode = UserLevels.Normal;

            if (user == null)
            {
                // 检查是否为超级管理员
                if (_adminService.IsSuperAdminByName(result.Nickname))
                {
                    levelCode = UserLevels.Admin;
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

                // 如果是超级管理员，同时创建管理员记录（批量保存）
                if (levelCode == UserLevels.Admin)
                {
                    var admin = new TacticsAdminModel
                    {
                        UserId = user.Id,
                        Role = AdminRoles.SuperAdmin,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TacticsAdmins.Add(admin);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("新用户注册: {Provider}, OAuthId={OAuthId}, UserId={UserId}",
                    provider, result.OAuthId, user.Id);
            }
            else
            {
                levelCode = user.LevelCode;

                // 更新用户信息（仅在值变化时更新）
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
            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            return Ok(new
            {
                message = "登录成功",
                userId = user.Id,
                nickname = user.Nickname,
                avatarUrl = user.AvatarUrl,
                levelCode = user.LevelCode,
                accessToken,
                refreshToken,
                expiresIn = _jwtConfig.AccessTokenExpiryHours * 3600
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
            .FirstOrDefaultAsync(l => l.LevelCode == UserLevels.Normal);

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

    #region 测试端点（仅开发模式）

#if DEBUG
    /// <summary>
    /// 测试配装码编解码（仅开发模式）
    /// </summary>
    [HttpGet("TestShareCode")]
    public IActionResult TestShareCode([FromQuery] long? id, [FromQuery] string? code)
    {
        var result = new Dictionary<string, object>();

        // 测试ID编码
        if (id.HasValue)
        {
            var encoded = ShareCodeUtil.Encode(id.Value);
            result["input_id"] = id.Value;
            result["encoded_code"] = encoded ?? "无效ID";
            result["decoded_back"] = encoded != null ? ShareCodeUtil.Decode(encoded) : null;
        }

        // 测试代码解码
        if (!string.IsNullOrEmpty(code))
        {
            var decoded = ShareCodeUtil.Decode(code);
            result["input_code"] = code;
            result["decoded_id"] = decoded?.ToString() ?? "无效代码";
            result["valid"] = ShareCodeUtil.IsValid(code);
        }

        // 批量测试验证
        var testCases = new[]
        {
            (Id: 1L, ExpectedCode: "00000000"),
            (Id: 62L, ExpectedCode: "0000000z"),
            (Id: 63L, ExpectedCode: "00000010"),
            (Id: 3844L, ExpectedCode: "00000100"), // 62^2
            (Id: 218_340_105_584_896L, ExpectedCode: "zzzzzzzz") // 最大值
        };

        var testResults = testCases.Select(tc => new
        {
            tc.Id,
            Encoded = ShareCodeUtil.Encode(tc.Id),
            ExpectedCode = tc.ExpectedCode,
            DecodedBack = ShareCodeUtil.Encode(tc.Id) != null ? ShareCodeUtil.Decode(ShareCodeUtil.Encode(tc.Id)!) : null,
            RoundtripMatch = ShareCodeUtil.Encode(tc.Id) != null && ShareCodeUtil.Decode(ShareCodeUtil.Encode(tc.Id)!) == tc.Id
        }).ToList();

        result["test_cases"] = testResults;

        return Ok(result);
    }

    /// <summary>
    /// 测试文件解析器（仅开发模式）
    /// </summary>
    [HttpPost("TestParser")]
    public IActionResult TestParser()
    {
        // 神族测试用例
        var protossTestJson = @"{
            ""name"": ""PvZ快攻战术"",
            ""author"": ""TestAuthor"",
            ""actions"": [
                { ""itemAbbr"": ""Pylon"", ""time"": 0 },
                { ""itemAbbr"": ""Gateway"", ""time"": 30 }
            ]
        }";

        // 人族测试用例
        var terranTestJson = @"{
            ""name"": ""TvP防守反击"",
            ""author"": ""TerranMaster"",
            ""actions"": [
                { ""itemAbbr"": ""SupplyDepot"", ""time"": 0 },
                { ""itemAbbr"": ""Barracks"", ""time"": 30 }
            ]
        }";

        // 虫族测试用例
        var zergTestJson = @"{
            ""name"": ""ZvT快攻"",
            ""author"": ""ZergPlayer"",
            ""actions"": [
                { ""itemAbbr"": ""SpawningPool"", ""time"": 0 }
            ]
        }";

        var parser = new TacticsFileParser();

        var results = new[]
        {
            (Name: "神族测试", Json: protossTestJson),
            (Name: "人族测试", Json: terranTestJson),
            (Name: "虫族测试", Json: zergTestJson)
        }.Select(r =>
        {
            var parseResult = parser.Parse(r.Json);  // 只解析1次
            return new
            {
                testName = r.Name,
                success = parseResult.Success,
                name = parseResult.Name,
                author = parseResult.Author,
                race = parseResult.Race,
                raceDisplay = Races.GetDisplayName(parseResult.Race)
            };
        }).ToList();

        return Ok(new
        {
            testResults = results,
            summary = new
            {
                allPassed = results.All(r => r.success),
                raceDetectionWorking = results.All(r => r.race != null)
            }
        });
    }

    /// <summary>
    /// 测试哈希工具（仅开发模式）
    /// </summary>
    [HttpGet("TestHash")]
    public IActionResult TestHash([FromQuery] string? content)
    {
        if (string.IsNullOrEmpty(content))
        {
            content = "Hello, TactiX!";
        }

        var hash = HashUtil.ComputeSha256(content);
        var hashBytes = HashUtil.ComputeSha256(System.Text.Encoding.UTF8.GetBytes(content));

        return Ok(new
        {
            content,
            hash,
            hashBytes,
            length = hash.Length,
            formatCorrect = hash.Length == 64 && hash.All(c => char.IsLower(c) || char.IsDigit(c)),
            consistency = hash == hashBytes
        });
    }

    /// <summary>
    /// 测试文件验证器（仅开发模式）
    /// </summary>
    [HttpPost("TestValidator")]
    public async Task<IActionResult> TestValidator([FromBody] TestValidatorRequest? request)
    {
        request ??= new TestValidatorRequest();

        var testContent = request.Content ?? @"{
            ""name"": ""测试战术"",
            ""author"": ""测试作者"",
            ""description"": ""这是一个正常的战术文件"",
            ""actions"": [
                { ""itemAbbr"": ""Pylon"", ""supply"": ""13/15"", ""time"": 100 },
                { ""itemAbbr"": ""Gateway"", ""supply"": ""16/17"", ""time"": 150 }
            ]
        }";

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(testContent);
        var validator = HttpContext.RequestServices.GetRequiredService<IFileSecurityValidator>();
        var result = await validator.ValidateAsync(contentBytes, "test.tactix");

        return Ok(new
        {
            isValid = result.IsValid,
            failedLayer = result.FailedLayer,
            errors = result.Errors,
            warnings = result.Warnings,
            fileHash = result.FileHash,
            hashLength = result.FileHash?.Length,
            detectedRace = result.DetectedRace,
            raceDisplay = Races.GetDisplayName(result.DetectedRace),
            name = result.Name,
            author = result.Author
        });
    }

    /// <summary>
    /// 测试XSS拦截（仅开发模式）
    /// </summary>
    [HttpPost("TestXssBlock")]
    public async Task<IActionResult> TestXssBlock()
    {
        var maliciousContent = @"{
            ""name"": ""恶意战术<script>alert('xss')</script>"",
            ""author"": ""黑客"",
            ""actions"": [
                { ""itemAbbr"": ""Pylon"" }
            ]
        }";

        var contentBytes = System.Text.Encoding.UTF8.GetBytes(maliciousContent);
        var validator = HttpContext.RequestServices.GetRequiredService<IFileSecurityValidator>();
        var result = await validator.ValidateAsync(contentBytes, "malicious.tactix");

        return Ok(new
        {
            isValid = result.IsValid,
            shouldFail = !result.IsValid,
            failedLayer = result.FailedLayer,
            expectedLayer = 4,
            errors = result.Errors,
            blockedXss = result.Errors.Any(e => e.Contains("XSS"))
        });
    }
#endif

    #endregion
}

#if DEBUG
/// <summary>
/// 测试验证器请求
/// </summary>
public class TestValidatorRequest
{
    public string? Content { get; set; }
}
#endif