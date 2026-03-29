using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 用户点赞表模型
/// </summary>
[Table("tactics_like")]
public class TacticsLikeModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("file_id")]
    public long FileId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // 导航属性
    public virtual TacticsUserModel? User { get; set; }
    public virtual TacticsFileModel? File { get; set; }
}
