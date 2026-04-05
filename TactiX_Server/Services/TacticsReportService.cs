using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Resp;
using TactiX_Server.Models.Tactics;
using TactiX_Server.Utils;

namespace TactiX_Server.Services;

/// <summary>
/// 举报原因常量
/// </summary>
public static class ReportReasons
{
    public const string Inappropriate = "inappropriate";   // 不当内容
    public const string Copyright = "copyright";           // 侵权
    public const string Malicious = "malicious";           // 恶意代码
    public const string Spam = "spam";                     // 垃圾内容
    public const string Other = "other";                   // 其他

    public static readonly string[] All = { Inappropriate, Copyright, Malicious, Spam, Other };

    public static string GetDisplayName(string reason) => reason switch
    {
        Inappropriate => "不当内容",
        Copyright => "版权侵权",
        Malicious => "恶意代码",
        Spam => "垃圾内容",
        Other => "其他原因",
        _ => reason
    };

    public static bool IsValid(string reason) => All.Contains(reason);
}

/// <summary>
/// 举报状态常量
/// </summary>
public static class ReportStatus
{
    public const string Pending = "pending";       // 待处理
    public const string Processed = "processed";   // 已处理（删除文件）
    public const string Ignored = "ignored";       // 已忽略

    public static string GetDisplayName(string status) => status switch
    {
        Pending => "待处理",
        Processed => "已处理",
        Ignored => "已忽略",
        _ => status
    };
}

/// <summary>
/// 举报操作结果
/// </summary>
public class ReportResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static ReportResult Success() => new() { Succeeded = true };
    public static ReportResult Failed(string message) => new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>
/// 举报服务接口
/// </summary>
public interface ITacticsReportService
{
    /// <summary>用户提交举报</summary>
    Task<ReportResult> SubmitReportAsync(long reporterId, string shareCode, string reason, string? description);

    /// <summary>获取用户举报历史</summary>
    Task<UserReportListResponse> GetUserReportsAsync(long userId, int page, int pageSize);

    /// <summary>检查用户是否已举报某文件</summary>
    Task<bool> HasUserReportedAsync(long userId, long fileId);

    /// <summary>管理员：获取待处理举报列表</summary>
    Task<ReportListResponse> GetPendingReportsAsync(int page, int pageSize);

    /// <summary>管理员：获取举报详情</summary>
    Task<ReportDetailResponse?> GetReportDetailAsync(long reportId);

    /// <summary>管理员：处理举报</summary>
    Task<ReportResult> ProcessReportAsync(long adminId, long reportId, bool takeAction, string? handleResult);

    /// <summary>管理员：获取举报统计</summary>
    Task<ReportStatsResponse> GetReportStatsAsync();
}

/// <summary>
/// 举报服务实现
/// </summary>
public class TacticsReportService : ITacticsReportService
{
    private readonly TacticsDbContext _context;
    private readonly IAdminService _adminService;
    private readonly ILogger<TacticsReportService> _logger;

    public TacticsReportService(
        TacticsDbContext context,
        IAdminService adminService,
        ILogger<TacticsReportService> logger)
    {
        _context = context;
        _adminService = adminService;
        _logger = logger;
    }

    public async Task<ReportResult> SubmitReportAsync(long reporterId, string shareCode, string reason, string? description)
    {
        // 验证举报原因
        if (!ReportReasons.IsValid(reason))
        {
            return ReportResult.Failed($"无效的举报原因: {reason}");
        }

        // 解析配装码获取文件ID
        var fileId = ShareCodeUtil.Decode(shareCode);
        if (!fileId.HasValue || fileId.Value <= 0)
        {
            return ReportResult.Failed("无效的配装码");
        }

        var fileIdValue = fileId.Value;

        // 检查文件是否存在
        var file = await _context.TacticsFiles.FindAsync(fileIdValue);
        if (file == null)
        {
            return ReportResult.Failed("文件不存在");
        }

        // 检查用户是否已举报该文件
        if (await HasUserReportedAsync(reporterId, fileIdValue))
        {
            return ReportResult.Failed("您已举报过该文件");
        }

        // 创建举报记录
        var report = new TacticsReportModel
        {
            FileId = fileIdValue,
            ReporterId = reporterId,
            Reason = reason,
            Description = description,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TacticsReports.Add(report);
        await _context.SaveChangesAsync();

        _logger.LogInformation("用户 {ReporterId} 举报文件 {ShareCode}, 原因: {Reason}", reporterId, shareCode, reason);

        return ReportResult.Success();
    }

    public async Task<UserReportListResponse> GetUserReportsAsync(long userId, int page, int pageSize)
    {
        var query = _context.TacticsReports
            .AsNoTracking()
            .Where(r => r.ReporterId == userId)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        var reports = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new UserReportItemResponse
            {
                Id = r.Id,
                ShareCode = r.File!.ShareCode,
                FileName = r.File.Name,
                ReasonDisplay = ReportReasons.GetDisplayName(r.Reason),
                StatusDisplay = ReportStatus.GetDisplayName(r.Status),
                CreatedAt = r.CreatedAt,
                HandleResult = r.HandleResult
            })
            .ToListAsync();

        return new UserReportListResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Reports = reports
        };
    }

    public async Task<bool> HasUserReportedAsync(long userId, long fileId)
    {
        return await _context.TacticsReports
            .AnyAsync(r => r.ReporterId == userId && r.FileId == fileId);
    }

    public async Task<ReportListResponse> GetPendingReportsAsync(int page, int pageSize)
    {
        var query = _context.TacticsReports
            .AsNoTracking()
            .Where(r => r.Status == ReportStatus.Pending)
            .OrderByDescending(r => r.CreatedAt);

        var totalCount = await query.CountAsync();

        var reports = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportItemResponse
            {
                Id = r.Id,
                ShareCode = r.File!.ShareCode,
                FileName = r.File.Name,
                Reason = r.Reason,
                ReasonDisplay = ReportReasons.GetDisplayName(r.Reason),
                Description = r.Description,
                Status = r.Status,
                StatusDisplay = ReportStatus.GetDisplayName(r.Status),
                ReporterNickname = r.Reporter!.Nickname,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new ReportListResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Reports = reports
        };
    }

    public async Task<ReportDetailResponse?> GetReportDetailAsync(long reportId)
    {
        var report = await _context.TacticsReports
            .AsNoTracking()
            .Include(r => r.File)
            .Include(r => r.Reporter)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
        {
            return null;
        }

        UserBriefResponse? handler = null;
        if (report.HandledBy.HasValue)
        {
            var admin = await _context.TacticsAdmins
                .AsNoTracking()
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == report.HandledBy.Value);

            if (admin?.User != null)
            {
                handler = new UserBriefResponse
                {
                    Id = admin.User.Id,
                    Nickname = admin.User.Nickname,
                    AvatarUrl = admin.User.AvatarUrl
                };
            }
        }

        string? uploaderNickname = null;
        if (report.File != null)
        {
            var uploader = await _context.TacticsUsers
                .AsNoTracking()
                .Where(u => u.Id == report.File.UploaderId)
                .Select(u => u.Nickname)
                .FirstOrDefaultAsync();
            uploaderNickname = uploader;
        }

        return new ReportDetailResponse
        {
            Id = report.Id,
            File = report.File != null ? new ReportFileResponse
            {
                ShareCode = report.File.ShareCode,
                Name = report.File.Name,
                Author = report.File.Author,
                Race = report.File.Race,
                UploaderNickname = uploaderNickname
            } : null,
            Reason = report.Reason,
            ReasonDisplay = ReportReasons.GetDisplayName(report.Reason),
            Description = report.Description,
            Status = report.Status,
            StatusDisplay = ReportStatus.GetDisplayName(report.Status),
            Reporter = report.Reporter != null ? new UserBriefResponse
            {
                Id = report.Reporter.Id,
                Nickname = report.Reporter.Nickname,
                AvatarUrl = report.Reporter.AvatarUrl,
                LevelCode = report.Reporter.LevelCode
            } : null,
            Handler = handler,
            HandleResult = report.HandleResult,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }

    public async Task<ReportResult> ProcessReportAsync(long adminId, long reportId, bool takeAction, string? handleResult)
    {
        var report = await _context.TacticsReports
            .Include(r => r.File)
            .FirstOrDefaultAsync(r => r.Id == reportId);

        if (report == null)
        {
            return ReportResult.Failed("举报记录不存在");
        }

        if (report.Status != ReportStatus.Pending)
        {
            return ReportResult.Failed("该举报已处理");
        }

        // 获取管理员记录
        var admin = await _adminService.GetAdminAsync(adminId);
        if (admin == null)
        {
            return ReportResult.Failed("管理员信息不存在");
        }

        // 更新举报状态
        report.Status = takeAction ? ReportStatus.Processed : ReportStatus.Ignored;
        report.HandledBy = admin.Id;
        report.HandleResult = handleResult;
        report.UpdatedAt = DateTime.UtcNow;

        // 如果采取行动，删除文件
        if (takeAction && report.File != null)
        {
            // 删除文件物理存储
            var filePath = Path.Combine("TacticsFiles", report.File.FilePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogInformation("删除文件: {FilePath}", filePath);
            }

            // 删除数据库记录（级联删除版本、点赞、收藏、评论）
            _context.TacticsFiles.Remove(report.File);
            _logger.LogInformation("删除文件记录: {ShareCode}", report.File.ShareCode);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("管理员 {AdminId} 处理举报 {ReportId}, 动作: {TakeAction}, 结果: {HandleResult}",
            adminId, reportId, takeAction ? "删除文件" : "忽略", handleResult);

        return ReportResult.Success();
    }

    public async Task<ReportStatsResponse> GetReportStatsAsync()
    {
        var pendingCount = await _context.TacticsReports.CountAsync(r => r.Status == ReportStatus.Pending);
        var processedCount = await _context.TacticsReports.CountAsync(r => r.Status == ReportStatus.Processed);
        var ignoredCount = await _context.TacticsReports.CountAsync(r => r.Status == ReportStatus.Ignored);

        return new ReportStatsResponse
        {
            PendingCount = pendingCount,
            ProcessedCount = processedCount,
            IgnoredCount = ignoredCount,
            TotalCount = pendingCount + processedCount + ignoredCount
        };
    }
}