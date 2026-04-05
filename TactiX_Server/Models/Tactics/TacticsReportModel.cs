using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 举报记录模型
/// </summary>
[Table("tactics_report")]
public class TacticsReportModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>被举报的文件ID</summary>
    [Column("file_id")]
    public long FileId { get; set; }

    /// <summary>举报用户ID</summary>
    [Column("reporter_id")]
    public long ReporterId { get; set; }

    /// <summary>举报原因类型</summary>
    [Column("reason")]
    [Required]
    [StringLength(32)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>举报详细描述</summary>
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>处理状态: pending/processed/ignored</summary>
    [Column("status")]
    [Required]
    [StringLength(16)]
    public string Status { get; set; } = "pending";

    /// <summary>处理管理员ID</summary>
    [Column("handled_by")]
    public long? HandledBy { get; set; }

    /// <summary>处理结果说明</summary>
    [Column("handle_result")]
    public string? HandleResult { get; set; }

    /// <summary>创建时间</summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    /// <summary>关联的文件</summary>
    [ForeignKey(nameof(FileId))]
    public virtual TacticsFileModel? File { get; set; }

    /// <summary>关联的举报者</summary>
    [ForeignKey(nameof(ReporterId))]
    public virtual TacticsUserModel? Reporter { get; set; }
}