using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 通知配置表模型
/// </summary>
[Table("notification_config")]
public class NotificationConfigModel
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("config_key")]
    [Required]
    [StringLength(64)]
    public string ConfigKey { get; set; } = string.Empty;

    [Column("config_value")]
    public string? ConfigValue { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
