using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TactiX_Server.Models.Req;
using TactiX_Server.Models.Resp;
using TactiX_Server.Services;

namespace TactiX_Server.Controllers;

/// <summary>
/// 举报控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly ITacticsReportService _reportService;
    private readonly IAdminService _adminService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        ITacticsReportService reportService,
        IAdminService adminService,
        ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _adminService = adminService;
        _logger = logger;
    }

    /// <summary>
    /// 提交举报
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> SubmitReport([FromBody] SubmitReportRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var result = await _reportService.SubmitReportAsync(
            userId.Value,
            request.ShareCode,
            request.Reason,
            request.Description);

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new SubmitReportResponse
        {
            Message = "举报提交成功，我们会尽快处理"
        });
    }

    /// <summary>
    /// 用户举报历史
    /// </summary>
    [HttpGet("MyReports")]
    [Authorize]
    public async Task<IActionResult> GetMyReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var response = await _reportService.GetUserReportsAsync(userId.Value, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// 管理员获取待处理举报列表
    /// </summary>
    [HttpGet("Pending")]
    [Authorize]
    public async Task<IActionResult> GetPendingReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        // 检查管理员权限
        if (!await _adminService.IsAdminAsync(userId.Value))
        {
            return Forbid();
        }

        var response = await _reportService.GetPendingReportsAsync(page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// 管理员获取举报详情
    /// </summary>
    [HttpGet("{id:long}")]
    [Authorize]
    public async Task<IActionResult> GetReportDetail(long id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        // 检查管理员权限
        if (!await _adminService.IsAdminAsync(userId.Value))
        {
            return Forbid();
        }

        var response = await _reportService.GetReportDetailAsync(id);
        if (response == null)
        {
            return NotFound(new { error = "举报记录不存在" });
        }

        return Ok(response);
    }

    /// <summary>
    /// 管理员处理举报
    /// </summary>
    [HttpPost("{id:long}/Process")]
    [Authorize]
    public async Task<IActionResult> ProcessReport(long id, [FromBody] ProcessReportRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        // 检查管理员权限
        if (!await _adminService.IsAdminAsync(userId.Value))
        {
            return Forbid();
        }

        var result = await _reportService.ProcessReportAsync(
            userId.Value,
            id,
            request.TakeAction,
            request.HandleResult);

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = request.TakeAction ? "举报已处理，相关文件已删除" : "举报已忽略" });
    }

    /// <summary>
    /// 管理员获取举报统计
    /// </summary>
    [HttpGet("Stats")]
    [Authorize]
    public async Task<IActionResult> GetStats()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        // 检查管理员权限
        if (!await _adminService.IsAdminAsync(userId.Value))
        {
            return Forbid();
        }

        var response = await _reportService.GetReportStatsAsync();
        return Ok(response);
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}