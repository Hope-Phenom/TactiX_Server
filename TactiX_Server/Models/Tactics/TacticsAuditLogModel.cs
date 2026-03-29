using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 审核记录表模型
/// </summary>
[Table("tactics_audit_log")]
public class TacticsAuditLogModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("file_id")]
    public long FileId { get; set; }

    [Column("admin_id")]
    public long AdminId { get; set; }

    [Column("old_status")]
    [StringLength(16)]
    public string? OldStatus { get; set; }

    [Column("new_status")]
    [Required]
    [StringLength(16)]
    public string NewStatus { get; set; } = string.Empty;

    [Column("reason")]
    public string? Reason { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // 导航属性
    public virtual TacticsFileModel? File { get; set; }
    public virtual TacticsAdminModel? Admin { get; set; }
}
