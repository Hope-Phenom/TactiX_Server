using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Resp;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// 通知类型常量
/// </summary>
public static class NotificationTypes
{
    public const string FileApproved = "file_approved";       // 文件审核通过
    public const string FileRejected = "file_rejected";       // 文件审核拒绝
    public const string CommentReply = "comment_reply";       // 评论被回复
    public const string FileLiked = "file_liked";             // 文件被点赞
    public const string ReportProcessed = "report_processed"; // 举报处理结果

    public static readonly string[] All = { FileApproved, FileRejected, CommentReply, FileLiked, ReportProcessed };

    public static string GetDisplayName(string type) => type switch
    {
        FileApproved => "审核通过",
        FileRejected => "审核拒绝",
        CommentReply => "评论回复",
        FileLiked => "获得点赞",
        ReportProcessed => "举报处理",
        _ => type
    };

    public static bool IsValid(string type) => All.Contains(type);
}

/// <summary>
/// 通知操作结果
/// </summary>
public class NotificationResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static NotificationResult Success() => new() { Succeeded = true };
    public static NotificationResult Failed(string message) => new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>
/// 通知服务接口
/// </summary>
public interface ITacticsNotificationService
{
    /// <summary>创建通知</summary>
    Task CreateNotificationAsync(long userId, string type, string title,
        string? content, long? relatedFileId, long? relatedCommentId, long? relatedUserId);

    /// <summary>批量创建通知</summary>
    Task CreateBatchNotificationsAsync(List<long> userIds, string type,
        string title, string? content, long? relatedFileId);

    /// <summary>获取用户通知列表</summary>
    Task<NotificationListResponse> GetNotificationsAsync(long userId, bool? unreadOnly, int page, int pageSize);

    /// <summary>标记单个通知已读</summary>
    Task<NotificationResult> MarkAsReadAsync(long userId, long notificationId);

    /// <summary>标记所有通知已读</summary>
    Task<NotificationResult> MarkAllAsReadAsync(long userId);

    /// <summary>获取未读通知数量</summary>
    Task<int> GetUnreadCountAsync(long userId);

    /// <summary>删除通知</summary>
    Task<NotificationResult> DeleteNotificationAsync(long userId, long notificationId);
}

/// <summary>
/// 通知服务实现
/// </summary>
public class TacticsNotificationService : ITacticsNotificationService
{
    private readonly TacticsDbContext _context;
    private readonly ILogger<TacticsNotificationService> _logger;

    public TacticsNotificationService(
        TacticsDbContext context,
        ILogger<TacticsNotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateNotificationAsync(long userId, string type, string title,
        string? content, long? relatedFileId, long? relatedCommentId, long? relatedUserId)
    {
        if (!NotificationTypes.IsValid(type))
        {
            _logger.LogWarning("无效的通知类型: {Type}", type);
            return;
        }

        var notification = new TacticsNotificationModel
        {
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            RelatedFileId = relatedFileId,
            RelatedCommentId = relatedCommentId,
            RelatedUserId = relatedUserId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.TacticsNotifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogDebug("创建通知: 用户 {UserId}, 类型 {Type}, 标题 {Title}", userId, type, title);
    }

    public async Task CreateBatchNotificationsAsync(List<long> userIds, string type,
        string title, string? content, long? relatedFileId)
    {
        if (!NotificationTypes.IsValid(type))
        {
            _logger.LogWarning("无效的通知类型: {Type}", type);
            return;
        }

        var notifications = userIds.Distinct().Select(userId => new TacticsNotificationModel
        {
            UserId = userId,
            Type = type,
            Title = title,
            Content = content,
            RelatedFileId = relatedFileId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _context.TacticsNotifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        _logger.LogDebug("批量创建通知: {Count} 个用户, 类型 {Type}", userIds.Count, type);
    }

    public async Task<NotificationListResponse> GetNotificationsAsync(long userId, bool? unreadOnly, int page, int pageSize)
    {
        var query = _context.TacticsNotifications
            .AsNoTracking()
            .Include(n => n.RelatedFile)
            .Include(n => n.RelatedComment)
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
        {
            query = query.Where(n => !n.IsRead);
        }

        query = query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();

        var notifications = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // 获取触发用户昵称
        var relatedUserIds = notifications
            .Where(n => n.RelatedUserId.HasValue)
            .Select(n => n.RelatedUserId!.Value)
            .Distinct()
            .ToList();

        var relatedUsers = await _context.TacticsUsers
            .Where(u => relatedUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Nickname);

        var now = DateTime.UtcNow;

        var items = notifications.Select(n => new NotificationItemResponse
        {
            Id = n.Id,
            Type = n.Type,
            TypeDisplay = NotificationTypes.GetDisplayName(n.Type),
            Title = n.Title,
            Content = n.Content,
            RelatedShareCode = n.RelatedFile?.ShareCode,
            RelatedFileName = n.RelatedFile?.Name,
            RelatedUserNickname = n.RelatedUserId.HasValue && relatedUsers.ContainsKey(n.RelatedUserId.Value)
                ? relatedUsers[n.RelatedUserId.Value]
                : null,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            TimeDisplay = GetTimeDisplay(n.CreatedAt, now)
        }).ToList();

        return new NotificationListResponse
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Notifications = items
        };
    }

    public async Task<NotificationResult> MarkAsReadAsync(long userId, long notificationId)
    {
        var notification = await _context.TacticsNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
        {
            return NotificationResult.Failed("通知不存在");
        }

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return NotificationResult.Success();
    }

    public async Task<NotificationResult> MarkAllAsReadAsync(long userId)
    {
        var count = await _context.TacticsNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(n => n.SetProperty(x => x.IsRead, true));

        _logger.LogDebug("标记所有通知已读: 用户 {UserId}, 数量 {Count}", userId, count);

        return NotificationResult.Success();
    }

    public async Task<int> GetUnreadCountAsync(long userId)
    {
        return await _context.TacticsNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<NotificationResult> DeleteNotificationAsync(long userId, long notificationId)
    {
        var count = await _context.TacticsNotifications
            .Where(n => n.Id == notificationId && n.UserId == userId)
            .ExecuteDeleteAsync();

        return count > 0
            ? NotificationResult.Success()
            : NotificationResult.Failed("通知不存在");
    }

    /// <summary>
    /// 获取时间显示文本
    /// </summary>
    private static string GetTimeDisplay(DateTime createdAt, DateTime now)
    {
        var diff = now - createdAt;

        if (diff.TotalMinutes < 1)
        {
            return "刚刚";
        }
        else if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes}分钟前";
        }
        else if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}小时前";
        }
        else if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}天前";
        }
        else
        {
            return createdAt.ToString("MM-dd HH:mm");
        }
    }
}