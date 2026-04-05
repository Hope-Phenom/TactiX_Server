namespace TactiX_Server.Models.Resp;

/// <summary>
/// 点赞Toggle响应
/// </summary>
public class LikeToggleResponse
{
    /// <summary>操作后的最终状态（true=已点赞，false=已取消）</summary>
    public bool IsLiked { get; set; }

    /// <summary>更新后的点赞总数</summary>
    public uint LikeCount { get; set; }
}

/// <summary>
/// 收藏Toggle响应
/// </summary>
public class FavoriteToggleResponse
{
    /// <summary>操作后的最终状态（true=已收藏，false=已取消）</summary>
    public bool IsFavorited { get; set; }

    /// <summary>更新后的收藏总数</summary>
    public uint FavoriteCount { get; set; }
}

/// <summary>
/// 用户点赞/收藏文件列表响应
/// </summary>
public class UserInteractionListResponse
{
    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>文件列表</summary>
    public List<TacticsBriefResponse> Files { get; set; } = new();
}

/// <summary>
/// 评论列表响应
/// </summary>
public class CommentListResponse
{
    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>评论列表</summary>
    public List<CommentResponse> Comments { get; set; } = new();
}

/// <summary>
/// 单条评论响应
/// </summary>
public class CommentResponse
{
    /// <summary>评论ID</summary>
    public long Id { get; set; }

    /// <summary>评论内容（已删除时显示"[已删除]"）</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>是否已删除</summary>
    public bool IsDeleted { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>评论作者信息</summary>
    public UserBriefResponse? Author { get; set; }

    /// <summary>父评论ID</summary>
    public long? ParentCommentId { get; set; }

    /// <summary>子评论列表（嵌套回复）</summary>
    public List<CommentResponse> Replies { get; set; } = new();
}

/// <summary>
/// 添加评论响应
/// </summary>
public class AddCommentResponse
{
    /// <summary>评论ID</summary>
    public long CommentId { get; set; }

    /// <summary>评论内容</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>父评论ID</summary>
    public long? ParentCommentId { get; set; }
}