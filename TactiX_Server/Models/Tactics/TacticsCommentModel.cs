using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 评论记录模型
/// </summary>
[Table("tactics_comment")]
public class TacticsCommentModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>评论用户ID</summary>
    [Column("user_id")]
    public long UserId { get; set; }

    /// <summary>评论的文件ID</summary>
    [Column("file_id")]
    public long FileId { get; set; }

    /// <summary>父评论ID（用于嵌套回复，最多2层）</summary>
    [Column("parent_comment_id")]
    public long? ParentCommentId { get; set; }

    /// <summary>评论内容</summary>
    [Column("content")]
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;

    /// <summary>是否已删除（软删除）</summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>创建时间</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // 导航属性
    [ForeignKey(nameof(UserId))]
    public virtual TacticsUserModel? User { get; set; }

    [ForeignKey(nameof(FileId))]
    public virtual TacticsFileModel? File { get; set; }

    [ForeignKey(nameof(ParentCommentId))]
    public virtual TacticsCommentModel? ParentComment { get; set; }

    /// <summary>子评论列表</summary>
    public virtual ICollection<TacticsCommentModel> Replies { get; set; } = new List<TacticsCommentModel>();
}