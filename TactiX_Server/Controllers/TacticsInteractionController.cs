using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TactiX_Server.Models.Req;
using TactiX_Server.Models.Resp;
using TactiX_Server.Services;
using TactiX_Server.Utils;

namespace TactiX_Server.Controllers;

/// <summary>
/// 战术互动控制器（点赞、收藏、评论）
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TacticsInteractionController : ControllerBase
{
    private readonly ITacticsInteractionService _interactionService;
    private readonly ILogger<TacticsInteractionController> _logger;

    public TacticsInteractionController(
        ITacticsInteractionService interactionService,
        ILogger<TacticsInteractionController> logger)
    {
        _interactionService = interactionService;
        _logger = logger;
    }

    /// <summary>
    /// 点赞/取消点赞
    /// </summary>
    [HttpPost("Like/{shareCode}")]
    [Authorize]
    public async Task<IActionResult> ToggleLike(string shareCode)
    {
        if (!ShareCodeUtil.IsValid(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var result = await _interactionService.ToggleLikeAsync(userId.Value, shareCode);
        if (result == null)
        {
            return BadRequest(new { error = "操作失败，文件不存在或无权限" });
        }

        return Ok(result);
    }

    /// <summary>
    /// 收藏/取消收藏
    /// </summary>
    [HttpPost("Favorite/{shareCode}")]
    [Authorize]
    public async Task<IActionResult> ToggleFavorite(string shareCode)
    {
        if (!ShareCodeUtil.IsValid(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var result = await _interactionService.ToggleFavoriteAsync(userId.Value, shareCode);
        if (result == null)
        {
            return BadRequest(new { error = "操作失败，文件不存在" });
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取我点赞的文件列表
    /// </summary>
    [HttpGet("Liked")]
    [Authorize]
    public async Task<IActionResult> GetLikedFiles([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var result = await _interactionService.GetUserLikedFilesAsync(userId.Value, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// 获取我的收藏列表
    /// </summary>
    [HttpGet("Favorites")]
    [Authorize]
    public async Task<IActionResult> GetFavorites([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var result = await _interactionService.GetUserFavoritesAsync(userId.Value, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// 添加评论
    /// </summary>
    [HttpPost("Comment/{shareCode}")]
    [Authorize]
    public async Task<IActionResult> AddComment(string shareCode, [FromBody] AddCommentRequest request)
    {
        if (!ShareCodeUtil.IsValid(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var result = await _interactionService.AddCommentAsync(
            userId.Value,
            shareCode,
            request.Content,
            request.ParentCommentId);

        if (result == null)
        {
            return BadRequest(new { error = "评论失败，文件不存在或无权限" });
        }

        return Ok(result);
    }

    /// <summary>
    /// 获取文件评论列表
    /// </summary>
    [HttpGet("Comments/{shareCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(string shareCode, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!ShareCodeUtil.IsValid(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var result = await _interactionService.GetCommentsAsync(shareCode, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    [HttpDelete("Comment/{id:long}")]
    [Authorize]
    public async Task<IActionResult> DeleteComment(long id)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var result = await _interactionService.DeleteCommentAsync(userId.Value, id);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "删除成功" });
    }

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