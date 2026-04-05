using System.ComponentModel.DataAnnotations;

namespace TactiX_Server.Models.Req;

/// <summary>
/// 添加评论请求
/// </summary>
public class AddCommentRequest
{
    /// <summary>评论内容（1-1000字符）</summary>
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    /// <summary>父评论ID（可选，用于嵌套回复）</summary>
    public long? ParentCommentId { get; set; }
}

/// <summary>
/// 获取互动列表请求（分页）
/// </summary>
public class GetInteractionListRequest
{
    /// <summary>页码</summary>
    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    /// <summary>每页数量</summary>
    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}