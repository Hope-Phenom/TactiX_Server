using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 通知记录模型
/// </summary>
[Table("tactics_notification")]
public class TacticsNotificationModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>接收通知的用户ID</summary>
    [Column("user_id")]
    public long UserId { get; set; }

    /// <summary>通知类型</summary>
    [Column("type")]
    [Required]
    [StringLength(32)]
    public string Type { get; set; } = string.Empty;

    /// <summary>通知标题</summary>
    [Column("title")]
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>通知内容</summary>
    [Column("content")]
    public string? Content { get; set; }

    /// <summary>关联文件ID</summary>
    [Column("related_file_id")]
    public long? RelatedFileId { get; set; }

    /// <summary>关联评论ID</summary>
    [Column("related_comment_id")]
    public long? RelatedCommentId { get; set; }

    /// <summary>触发通知的用户ID</summary>
    [Column("related_user_id")]
    public long? RelatedUserId { get; set; }

    /// <summary>是否已读</summary>
    [Column("is_read")]
    public bool IsRead { get; set; }

    /// <summary>创建时间</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>关联的用户</summary>
    [ForeignKey(nameof(UserId))]
    public virtual TacticsUserModel? User { get; set; }

    /// <summary>关联的文件</summary>
    [ForeignKey(nameof(RelatedFileId))]
    public virtual TacticsFileModel? RelatedFile { get; set; }

    /// <summary>关联的评论</summary>
    [ForeignKey(nameof(RelatedCommentId))]
    public virtual TacticsCommentModel? RelatedComment { get; set; }
}