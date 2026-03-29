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

    [Column("user_id")]
    public long UserId { get; set; }

    [Column("role")]
    [Required]
    [StringLength(32)]
    public string Role { get; set; } = "moderator";

    [Column("granted_by")]
    public long? GrantedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // 导航属性
    public virtual TacticsUserModel? User { get; set; }

    // 判断是否为超级管理员的常量
    public const string SuperAdminIdentifier = "hope_phenom";
}
