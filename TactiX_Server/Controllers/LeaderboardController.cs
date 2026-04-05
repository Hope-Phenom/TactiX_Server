using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TactiX_Server.Services;

namespace TactiX_Server.Controllers;

/// <summary>
/// 排行榜控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LeaderboardController : ControllerBase
{
    private readonly ITacticsLeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        ITacticsLeaderboardService leaderboardService,
        ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// 获取热门战术排行榜
    /// </summary>
    [HttpGet("HotFiles")]
    [AllowAnonymous]
    public async Task<IActionResult> GetHotFiles(
        [FromQuery] string period = "weekly",
        [FromQuery] string? race = null,
        [FromQuery] string sortBy = "downloads",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var response = await _leaderboardService.GetHotFilesAsync(period, race, sortBy, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// 获取贡献者排行榜
    /// </summary>
    [HttpGet("TopUploaders")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTopUploaders(
        [FromQuery] string period = "monthly",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 20;

        var response = await _leaderboardService.GetTopUploadersAsync(period, page, pageSize);
        return Ok(response);
    }
}