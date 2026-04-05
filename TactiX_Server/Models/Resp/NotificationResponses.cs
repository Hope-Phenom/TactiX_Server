namespace TactiX_Server.Models.Resp;

/// <summary>
/// 通知列表响应
/// </summary>
public class NotificationListResponse
{
    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>通知列表</summary>
    public List<NotificationItemResponse> Notifications { get; set; } = new();
}

/// <summary>
/// 通知项响应
/// </summary>
public class NotificationItemResponse
{
    /// <summary>通知ID</summary>
    public long Id { get; set; }

    /// <summary>通知类型</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>通知类型显示名称</summary>
    public string TypeDisplay { get; set; } = string.Empty;

    /// <summary>通知标题</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>通知内容</summary>
    public string? Content { get; set; }

    /// <summary>关联文件配装码</summary>
    public string? RelatedShareCode { get; set; }

    /// <summary>关联文件名称</summary>
    public string? RelatedFileName { get; set; }

    /// <summary>触发通知用户昵称</summary>
    public string? RelatedUserNickname { get; set; }

    /// <summary>是否已读</summary>
    public bool IsRead { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>时间显示（如"刚刚"、"5分钟前"）</summary>
    public string? TimeDisplay { get; set; }
}

/// <summary>
/// 未读通知数量响应
/// </summary>
public class UnreadCountResponse
{
    /// <summary>未读数量</summary>
    public int UnreadCount { get; set; }
}