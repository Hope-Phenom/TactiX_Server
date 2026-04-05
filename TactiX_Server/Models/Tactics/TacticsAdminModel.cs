using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 管理员模型
/// </summary>
[Table("tactics_admin")]
public class TacticsAdminModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>关联的用户ID</summary>
    [Column("user_id")]
    public long UserId { get; set; }

    /// <summary>管理员角色: super_admin/admin/moderator</summary>
    [Column("role")]
    [StringLength(16)]
    public string Role { get; set; } = "moderator";

    /// <summary>授予者ID</summary>
    [Column("granted_by")]
    public long? GrantedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // 导航属性
    [ForeignKey(nameof(UserId))]
    public virtual TacticsUserModel? User { get; set; }
}