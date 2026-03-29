using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Security.Claims;

using TactiX_Server.Data;
using TactiX_Server.Models.Req;
using TactiX_Server.Models.Resp;
using TactiX_Server.Services;
using TactiX_Server.Utils;

namespace TactiX_Server.Controllers;

/// <summary>
/// 战术大厅控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TacticsHallController : ControllerBase
{
    private readonly ITacticsFileService _tacticsFileService;
    private readonly TacticsDbContext _context;
    private readonly ILogger<TacticsHallController> _logger;

    public TacticsHallController(
        ITacticsFileService tacticsFileService,
        TacticsDbContext context,
        ILogger<TacticsHallController> logger)
    {
        _tacticsFileService = tacticsFileService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 上传战术文件
    /// </summary>
    [HttpPost("Upload")]
    [Authorize]
    [RequestSizeLimit(104857600)] // 100MB 最大请求大小
    public async Task<IActionResult> Upload([FromForm] UploadTacticsRequest request, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "请上传文件" });
        }

        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _tacticsFileService.UploadFileAsync(userId, stream, file.FileName, request.Changelog);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new UploadTacticsResponse
            {
                ShareCode = result.ShareCode,
                FileId = result.FileId,
                VersionNumber = result.VersionNumber,
                Status = "pending",
                Message = "上传成功，文件正在审核中"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文件时发生异常");
            return StatusCode(500, new { error = "上传失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 上传战术文件新版本
    /// </summary>
    [HttpPost("UploadVersion")]
    [Authorize]
    [RequestSizeLimit(104857600)]
    public async Task<IActionResult> UploadVersion([FromForm] UploadVersionRequest request, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "请上传文件" });
        }

        if (string.IsNullOrWhiteSpace(request.ShareCode))
        {
            return BadRequest(new { error = "请提供配装码" });
        }

        if (!ShareCodeUtil.IsValidShareCode(request.ShareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _tacticsFileService.UploadVersionAsync(userId, request.ShareCode, stream, request.Changelog);

            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new UploadTacticsResponse
            {
                ShareCode = result.ShareCode,
                FileId = result.FileId,
                VersionNumber = result.VersionNumber,
                Status = "pending",
                Message = "新版本上传成功，文件正在审核中"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传版本时发生异常");
            return StatusCode(500, new { error = "上传失败，请稍后重试" });
        }
    }

    /// <summary>
    /// 获取战术文件详情
    /// </summary>
    [HttpGet("Detail/{shareCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDetail(string shareCode)
    {
        if (!ShareCodeUtil.IsValidShareCode(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var file = await _tacticsFileService.GetFileByShareCodeAsync(shareCode);

        if (file == null)
        {
            return NotFound(new { error = "战术文件不存在或未通过审核" });
        }

        // 获取当前版本号
        var version = await _context.TacticsFileVersions
            .AsNoTracking()
            .Where(v => v.FileId == file.Id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => v.VersionNumber)
            .FirstOrDefaultAsync();

        var response = new TacticsDetailResponse
        {
            ShareCode = file.ShareCode,
            Name = file.Name,
            Description = file.Description ?? string.Empty,
            AuthorName = file.AuthorName ?? string.Empty,
            RacePlayed = file.RacePlayed ?? "Unknown",
            RaceOpponent = file.RaceOpponent ?? "Unknown",
            Matchup = file.Matchup ?? string.Empty,
            TacticType = (int)file.TacticType,
            ModName = file.ModName,
            DownloadCount = (int)file.DownloadCount,
            LikeCount = (int)file.LikeCount,
            CurrentVersion = version,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt
        };

        if (file.Uploader != null)
        {
            response.Uploader = new UserBriefResponse
            {
                Id = file.Uploader.Id,
                Nickname = file.Uploader.Nickname ?? "未知用户",
                AvatarUrl = file.Uploader.AvatarUrl ?? string.Empty,
                LevelCode = file.Uploader.LevelCode
            };
        }

        return Ok(response);
    }

    /// <summary>
    /// 搜索战术文件
    /// </summary>
    [HttpGet("Search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] SearchTacticsRequest request)
    {
        var query = _context.TacticsFiles
            .AsNoTracking()
            .Where(f => f.Status == "approved")
            .AsQueryable();

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(f =>
                EF.Functions.Like(f.Name, $"%{keyword}%") ||
                EF.Functions.Like(f.Description ?? "", $"%{keyword}%"));
        }

        // 种族过滤
        if (!string.IsNullOrWhiteSpace(request.RacePlayed))
        {
            query = query.Where(f => f.RacePlayed == request.RacePlayed.ToUpper());
        }

        if (!string.IsNullOrWhiteSpace(request.RaceOpponent))
        {
            query = query.Where(f => f.RaceOpponent == request.RaceOpponent.ToUpper());
        }

        // 对抗类型过滤
        if (!string.IsNullOrWhiteSpace(request.Matchup))
        {
            query = query.Where(f => f.Matchup == request.Matchup.ToUpper());
        }

        // 战术类型过滤
        if (request.TacticType.HasValue)
        {
            query = query.Where(f => f.TacticType == request.TacticType.Value);
        }

        // 排序
        query = request.SortBy?.ToLower() switch
        {
            "popular" => query.OrderByDescending(f => f.LikeCount),
            "downloads" => query.OrderByDescending(f => f.DownloadCount),
            _ => query.OrderByDescending(f => f.CreatedAt)
        };

        // 分页
        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(f => new TacticsListItemResponse
            {
                ShareCode = f.ShareCode,
                Name = f.Name,
                AuthorName = f.AuthorName ?? "未知作者",
                RacePlayed = f.RacePlayed ?? "Unknown",
                Matchup = f.Matchup ?? string.Empty,
                TacticType = (int)f.TacticType,
                DownloadCount = (int)f.DownloadCount,
                LikeCount = (int)f.LikeCount,
                CreatedAt = f.CreatedAt
            })
            .ToListAsync();

        return Ok(new PagedTacticsResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    /// <summary>
    /// 下载战术文件
    /// </summary>
    [HttpGet("Download/{shareCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(string shareCode, [FromQuery] int? version = null)
    {
        if (!ShareCodeUtil.IsValidShareCode(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var result = await _tacticsFileService.DownloadFileAsync(shareCode, version);

        if (result == null)
        {
            return NotFound(new { error = "战术文件不存在或未通过审核" });
        }

        if (!System.IO.File.Exists(result.FilePath))
        {
            return NotFound(new { error = "文件已被删除或移动" });
        }

        var fileStream = System.IO.File.OpenRead(result.FilePath);
        return File(fileStream, result.ContentType, result.FileName);
    }

    /// <summary>
    /// 获取战术文件版本列表
    /// </summary>
    [HttpGet("Versions/{shareCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVersions(string shareCode)
    {
        if (!ShareCodeUtil.IsValidShareCode(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var versions = await _tacticsFileService.GetFileVersionsAsync(shareCode);

        var response = versions.Select(v => new VersionInfoResponse
        {
            VersionNumber = (int)v.VersionNumber,
            TacVersion = (int)v.TacVersion,
            Changelog = v.Changelog ?? string.Empty,
            FileSize = v.FileSize,
            CreatedAt = v.CreatedAt
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// 删除战术文件
    /// </summary>
    [HttpDelete("Delete/{shareCode}")]
    [Authorize]
    public async Task<IActionResult> Delete(string shareCode)
    {
        if (!ShareCodeUtil.IsValidShareCode(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var result = await _tacticsFileService.DeleteFileAsync(userId, shareCode);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 点赞战术文件
    /// </summary>
    [HttpPost("Like/{shareCode}")]
    [Authorize]
    public async Task<IActionResult> Like(string shareCode)
    {
        if (!ShareCodeUtil.IsValidShareCode(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var userId = GetCurrentUserId();
        if (userId == 0)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        // 查找文件
        var file = await _context.TacticsFiles
            .FirstOrDefaultAsync(f => f.ShareCode == shareCode);

        if (file == null)
        {
            return NotFound(new { error = "战术文件不存在" });
        }

        // 检查是否已点赞
        var existingLike = await _context.TacticsLikes
            .FirstOrDefaultAsync(l => l.UserId == userId && l.FileId == file.Id);

        if (existingLike != null)
        {
            // 取消点赞
            _context.TacticsLikes.Remove(existingLike);
            file.LikeCount--;
        }
        else
        {
            // 添加点赞
            _context.TacticsLikes.Add(new Models.Tactics.TacticsLikeModel
            {
                UserId = userId,
                FileId = file.Id,
                CreatedAt = DateTime.UtcNow
            });
            file.LikeCount++;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            liked = existingLike == null,
            likeCount = file.LikeCount
        });
    }

    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    private long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            return 0;

        return long.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
