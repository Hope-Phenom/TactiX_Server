using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 用户等级配置模型
/// </summary>
[Table("user_level_config")]
public class UserLevelConfigModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    /// <summary>等级代码: normal/verified/pro/admin</summary>
    [Column("level_code")]
    [Required]
    [StringLength(32)]
    public string LevelCode { get; set; } = string.Empty;

    /// <summary>等级名称</summary>
    [Column("level_name")]
    [Required]
    [StringLength(64)]
    public string LevelName { get; set; } = string.Empty;

    /// <summary>等级描述</summary>
    [Column("description")]
    public string? Description { get; set; }

    // 上传限制
    [Column("max_file_size")]
    public uint MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

    [Column("max_upload_count")]
    public uint MaxUploadCount { get; set; } = 10;

    [Column("max_version_per_file")]
    public uint MaxVersionPerFile { get; set; } = 5;

    [Column("daily_upload_limit")]
    public uint DailyUploadLimit { get; set; } = 3;

    // 功能权限
    [Column("can_upload")]
    public bool CanUpload { get; set; } = true;

    [Column("can_delete_own_file")]
    public bool CanDeleteOwnFile { get; set; } = true;

    [Column("can_comment")]
    public bool CanComment { get; set; } = true;

    [Column("priority_review")]
    public bool PriorityReview { get; set; } = false;

    /// <summary>审核通知方式：true-即时通知，false-批量汇总</summary>
    [Column("instant_notification")]
    public bool InstantNotification { get; set; } = false;

    // UI展示
    [Column("badge_icon")]
    [StringLength(255)]
    public string? BadgeIcon { get; set; }

    [Column("badge_color")]
    [StringLength(32)]
    public string? BadgeColor { get; set; }

    [Column("show_in_leaderboard")]
    public bool ShowInLeaderboard { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
