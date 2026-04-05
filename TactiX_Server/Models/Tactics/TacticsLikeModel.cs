using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 点赞记录模型
/// </summary>
[Table("tactics_like")]
public class TacticsLikeModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>点赞用户ID</summary>
    [Column("user_id")]
    public long UserId { get; set; }

    /// <summary>点赞的文件ID</summary>
    [Column("file_id")]
    public long FileId { get; set; }

    /// <summary>点赞时间</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // 导航属性
    [ForeignKey(nameof(UserId))]
    public virtual TacticsUserModel? User { get; set; }

    [ForeignKey(nameof(FileId))]
    public virtual TacticsFileModel? File { get; set; }
}