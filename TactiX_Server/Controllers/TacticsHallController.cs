using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TactiX_Server.Data;
using TactiX_Server.Models.Req;
using TactiX_Server.Models.Resp;
using TactiX_Server.Models.Tactics;
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
    private readonly IAdminService _adminService;
    private readonly TacticsDbContext _context;
    private readonly ILogger<TacticsHallController> _logger;

    public TacticsHallController(
        ITacticsFileService tacticsFileService,
        IAdminService adminService,
        TacticsDbContext context,
        ILogger<TacticsHallController> logger)
    {
        _tacticsFileService = tacticsFileService;
        _adminService = adminService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// 上传战术文件
    /// </summary>
    [HttpPost("Upload")]
    [Authorize]
    [RequestSizeLimit(104857600)] // 100MB
    public async Task<IActionResult> Upload([FromForm] UploadTacticsRequest request, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "请上传文件" });
        }

        // 检查文件扩展名
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".tactix")
        {
            return BadRequest(new { error = "只支持.tactix格式文件" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _tacticsFileService.UploadFileAsync(userId.Value, stream, file.FileName, request.Changelog);

            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new UploadTacticsResponse
            {
                ShareCode = result.ShareCode!,
                FileId = result.FileId,
                VersionNumber = result.VersionNumber,
                Status = FileStatus.Pending,
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

        if (!ShareCodeUtil.IsValid(request.ShareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension != ".tactix")
        {
            return BadRequest(new { error = "只支持.tactix格式文件" });
        }

        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var result = await _tacticsFileService.UploadVersionAsync(userId.Value, request.ShareCode, stream, request.Changelog);

            if (!result.Succeeded)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(new UploadTacticsResponse
            {
                ShareCode = result.ShareCode!,
                FileId = result.FileId,
                VersionNumber = result.VersionNumber,
                Status = FileStatus.Pending,
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
        if (!ShareCodeUtil.IsValid(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var file = await _tacticsFileService.GetFileByShareCodeAsync(shareCode);

        if (file == null)
        {
            return NotFound(new { error = "战术文件不存在" });
        }

        // 只返回已审核通过的公开文件（管理员和上传者可查看自己的未审核文件）
        var userId = GetCurrentUserId();
        var isAdmin = userId.HasValue && await _adminService.IsAdminAsync(userId.Value);

        if (file.Status != FileStatus.Approved && !isAdmin && file.UploaderId != userId)
        {
            return NotFound(new { error = "战术文件不存在或未通过审核" });
        }

        // 获取当前版本号（直接查询最大版本号，避免加载全部版本）
        var fileId = ShareCodeUtil.Decode(shareCode);
        var currentVersion = fileId.HasValue
            ? await _context.TacticsFileVersions
                .AsNoTracking()
                .Where(v => v.FileId == fileId.Value)
                .MaxAsync(v => (int?)v.VersionNumber) ?? 1
            : 1;

        var response = new TacticsDetailResponse
        {
            ShareCode = file.ShareCode,
            Name = file.Name,
            Author = file.Author,
            Race = file.Race,
            RaceDisplay = Races.GetDisplayName(file.Race),
            FileSize = file.FileSize,
            DownloadCount = file.DownloadCount,
            LikeCount = file.LikeCount,
            CurrentVersion = currentVersion,
            Status = file.Status,
            CreatedAt = file.CreatedAt,
            UpdatedAt = file.UpdatedAt
        };

        if (file.Uploader != null)
        {
            var levelConfig = await _context.UserLevelConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.LevelCode == file.Uploader.LevelCode);

            response.Uploader = new UserBriefResponse
            {
                Id = file.Uploader.Id,
                Nickname = file.Uploader.Nickname,
                AvatarUrl = file.Uploader.AvatarUrl,
                LevelCode = file.Uploader.LevelCode,
                LevelName = levelConfig?.LevelName
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
            .Include(f => f.Uploader)
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.Status == FileStatus.Approved && f.IsPublic);

        // 关键词搜索
        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.ToLowerInvariant();
            query = query.Where(f =>
                (f.Name != null && f.Name.ToLower().Contains(keyword)) ||
                (f.Author != null && f.Author.ToLower().Contains(keyword)));
        }

        // 种族筛选
        if (!string.IsNullOrWhiteSpace(request.Race) && Races.IsValid(request.Race))
        {
            query = query.Where(f => f.Race == request.Race);
        }

        // 上传者筛选
        if (request.UploaderId.HasValue)
        {
            query = query.Where(f => f.UploaderId == request.UploaderId.Value);
        }

        // 排序
        query = request.SortBy?.ToLowerInvariant() switch
        {
            "popular" => query.OrderByDescending(f => f.LikeCount),
            "downloads" => query.OrderByDescending(f => f.DownloadCount),
            _ => query.OrderByDescending(f => f.CreatedAt) // latest
        };

        // 分页
        var totalCount = await query.CountAsync();
        var files = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var response = new SearchTacticsResponse
        {
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            Files = files.Select(f => new TacticsBriefResponse
            {
                ShareCode = f.ShareCode,
                Name = f.Name,
                Author = f.Author,
                Race = f.Race,
                RaceDisplay = Races.GetDisplayName(f.Race),
                DownloadCount = f.DownloadCount,
                LikeCount = f.LikeCount,
                CurrentVersion = f.Version,
                CreatedAt = f.CreatedAt,
                UploaderNickname = f.Uploader?.Nickname
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// 下载战术文件
    /// </summary>
    [HttpGet("Download/{shareCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> Download(string shareCode, [FromQuery] int? version = null)
    {
        if (!ShareCodeUtil.IsValid(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null)
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var file = await _context.TacticsFiles
            .AsNoTracking()
            .Where(f => f.Id == fileId.Value && !f.IsDeleted)
            .Select(f => new { f.FilePath, f.Status, f.UploaderId })
            .FirstOrDefaultAsync();

        if (file == null)
        {
            return NotFound(new { error = "文件不存在" });
        }

        // 权限检查：已审核通过的文件公开下载，管理员和上传者可下载未审核文件
        var userId = GetCurrentUserId();
        var isAdmin = userId.HasValue && await _adminService.IsAdminAsync(userId.Value);

        if (file.Status != FileStatus.Approved && !isAdmin && file.UploaderId != userId)
        {
            return NotFound(new { error = "文件不存在或未通过审核" });
        }

        // 确定要下载的版本
        string filePath;
        if (version.HasValue)
        {
            var versionPath = await _context.TacticsFileVersions
                .AsNoTracking()
                .Where(v => v.FileId == fileId.Value && v.VersionNumber == version.Value)
                .Select(v => v.FilePath)
                .FirstOrDefaultAsync();

            if (string.IsNullOrEmpty(versionPath))
            {
                return NotFound(new { error = "版本不存在" });
            }
            filePath = versionPath;
        }
        else
        {
            filePath = file.FilePath;
        }

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = "文件不存在" });
        }

        // 更新下载计数（仅对已审核通过的文件）
        if (file.Status == FileStatus.Approved)
        {
            try
            {
                await _context.TacticsFiles
                    .Where(f => f.Id == fileId.Value)
                    .ExecuteUpdateAsync(s => s.SetProperty(f => f.DownloadCount, f => f.DownloadCount + 1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "更新下载计数失败: FileId={FileId}", fileId.Value);
            }
        }

        // 使用流式传输
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(fileStream, "application/octet-stream", $"{shareCode}.tactix");
    }

    /// <summary>
    /// 获取文件版本列表
    /// </summary>
    [HttpGet("Versions/{shareCode}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVersions(string shareCode)
    {
        if (!ShareCodeUtil.IsValid(shareCode))
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var file = await _tacticsFileService.GetFileByShareCodeAsync(shareCode);
        if (file == null)
        {
            return NotFound(new { error = "战术文件不存在" });
        }

        // 权限检查：已审核通过的文件公开访问，管理员和上传者可访问未审核文件
        var userId = GetCurrentUserId();
        var isAdmin = userId.HasValue && await _adminService.IsAdminAsync(userId.Value);

        if (file.Status != FileStatus.Approved && !isAdmin && file.UploaderId != userId)
        {
            return NotFound(new { error = "文件不存在或未通过审核" });
        }

        var versions = await _tacticsFileService.GetFileVersionsAsync(shareCode);

        var response = new TacticsVersionListResponse
        {
            ShareCode = shareCode,
            Versions = versions.Select(v => new TacticsVersionResponse
            {
                VersionNumber = v.VersionNumber,
                FileSize = v.FileSize,
                Changelog = v.Changelog,
                CreatedAt = v.CreatedAt
            }).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// 删除战术文件
    /// </summary>
    [HttpDelete("Delete/{shareCode}")]
    [Authorize]
    public async Task<IActionResult> Delete(string shareCode)
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

        var result = await _tacticsFileService.DeleteFileAsync(userId.Value, shareCode);

        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "删除成功" });
    }

    /// <summary>
    /// 获取待审核文件列表（管理员）
    /// </summary>
    [HttpGet("PendingReview")]
    [Authorize]
    public async Task<IActionResult> GetPendingReview([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var isAdmin = await _adminService.IsAdminAsync(userId.Value);
        if (!isAdmin)
        {
            return Forbid();
        }

        var query = _context.TacticsFiles
            .Include(f => f.Uploader)
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.Status == FileStatus.Pending);

        var totalCount = await query.CountAsync();
        var files = await query
            .OrderBy(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var response = new
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Files = files.Select(f => new
            {
                f.Id,
                f.ShareCode,
                f.Name,
                f.Author,
                f.Race,
                RaceDisplay = Races.GetDisplayName(f.Race),
                f.FileSize,
                f.CreatedAt,
                UploaderId = f.UploaderId,
                UploaderNickname = f.Uploader?.Nickname
            })
        };

        return Ok(response);
    }

    /// <summary>
    /// 审核战术文件（管理员）
    /// </summary>
    [HttpPost("Review/{shareCode}")]
    [Authorize]
    public async Task<IActionResult> Review(string shareCode, [FromBody] ReviewRequest request)
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

        var isAdmin = await _adminService.IsAdminAsync(userId.Value);
        if (!isAdmin)
        {
            return Forbid();
        }

        var fileId = ShareCodeUtil.Decode(shareCode);
        if (fileId == null)
        {
            return BadRequest(new { error = "无效的配装码格式" });
        }

        var file = await _context.TacticsFiles.FirstOrDefaultAsync(f => f.Id == fileId.Value && !f.IsDeleted);
        if (file == null)
        {
            return NotFound(new { error = "战术文件不存在" });
        }

        var oldStatus = file.Status;
        file.Status = request.Approved ? FileStatus.Approved : FileStatus.Rejected;
        file.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "管理员 {AdminId} 审核文件 {ShareCode}: {OldStatus} -> {NewStatus}",
            userId, shareCode, oldStatus, file.Status);

        return Ok(new
        {
            message = request.Approved ? "审核通过" : "审核拒绝",
            shareCode,
            status = file.Status
        });
    }

    /// <summary>
    /// 批量审核战术文件（管理员）
    /// </summary>
    [HttpPost("BatchReview")]
    [Authorize]
    public async Task<IActionResult> BatchReview([FromBody] BatchReviewRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "未登录或登录已过期" });
        }

        var isAdmin = await _adminService.IsAdminAsync(userId.Value);
        if (!isAdmin)
        {
            return Forbid();
        }

        var results = new List<object>();
        foreach (var shareCode in request.ShareCodes)
        {
            var fileId = ShareCodeUtil.Decode(shareCode);
            if (fileId == null) continue;

            var file = await _context.TacticsFiles.FirstOrDefaultAsync(f => f.Id == fileId.Value && !f.IsDeleted);
            if (file == null) continue;

            file.Status = request.Approved ? FileStatus.Approved : FileStatus.Rejected;
            file.UpdatedAt = DateTime.UtcNow;

            results.Add(new { shareCode, status = file.Status });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "管理员 {AdminId} 批量审核 {Count} 个文件",
            userId, results.Count);

        return Ok(new { message = $"已审核 {results.Count} 个文件", results });
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