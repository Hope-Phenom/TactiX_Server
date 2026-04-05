using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TactiX_Server.Models.Resp;
using TactiX_Server.Services;

namespace TactiX_Server.Controllers;

/// <summary>
/// 通知控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    private readonly ITacticsNotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        ITacticsNotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取通知列表
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] bool? unreadOnly = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var response = await _notificationService.GetNotificationsAsync(userId.Value, unreadOnly, page, pageSize);
        return Ok(response);
    }

    /// <summary>
    /// 获取未读通知数量
    /// </summary>
    [HttpGet("UnreadCount")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var count = await _notificationService.GetUnreadCountAsync(userId.Value);
        return Ok(new UnreadCountResponse { UnreadCount = count });
    }

    /// <summary>
    /// 标记通知已读
    /// </summary>
    [HttpPost("{id:long}/Read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(long id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var result = await _notificationService.MarkAsReadAsync(userId.Value, id);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "已标记为已读" });
    }

    /// <summary>
    /// 标记所有通知已读
    /// </summary>
    [HttpPost("MarkAllRead")]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var result = await _notificationService.MarkAllAsReadAsync(userId.Value);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "全部已标记为已读" });
    }

    /// <summary>
    /// 删除通知
    /// </summary>
    [HttpDelete("{id:long}")]
    [Authorize]
    public async Task<IActionResult> DeleteNotification(long id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var result = await _notificationService.DeleteNotificationAsync(userId.Value, id);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "通知已删除" });
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