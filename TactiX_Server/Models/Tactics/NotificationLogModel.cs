using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 系统通知日志表模型
/// </summary>
[Table("notification_log")]
public class NotificationLogModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>通知类型: instant/digest</summary>
    [Column("notification_type")]
    [Required]
    [StringLength(16)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>事件类型</summary>
    [Column("event_type")]
    [Required]
    [StringLength(64)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>通知渠道: email/feishu</summary>
    [Column("channel")]
    [Required]
    [StringLength(16)]
    public string Channel { get; set; } = string.Empty;

    [Column("title")]
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>发送状态: pending/sent/failed</summary>
    [Column("status")]
    [Required]
    [StringLength(16)]
    public string Status { get; set; } = "pending";

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
