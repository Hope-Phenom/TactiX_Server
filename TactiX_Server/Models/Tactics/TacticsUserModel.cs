using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 战术大厅用户模型
/// </summary>
[Table("tactics_user")]
public class TacticsUserModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>OAuth提供商: qq/wechat/dev</summary>
    [Column("oauth_provider")]
    [Required]
    [StringLength(16)]
    public string OAuthProvider { get; set; } = string.Empty;

    /// <summary>OAuth平台用户唯一ID</summary>
    [Column("oauth_id")]
    [Required]
    [StringLength(64)]
    public string OAuthId { get; set; } = string.Empty;

    /// <summary>用户等级代码</summary>
    [Column("level_code")]
    [Required]
    [StringLength(32)]
    public string LevelCode { get; set; } = "normal";

    /// <summary>昵称</summary>
    [Column("nickname")]
    [StringLength(64)]
    public string? Nickname { get; set; }

    /// <summary>头像URL</summary>
    [Column("avatar_url")]
    [StringLength(255)]
    public string? AvatarUrl { get; set; }

    /// <summary>个人简介</summary>
    [Column("bio")]
    public string? Bio { get; set; }

    /// <summary>账号是否激活</summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // 统计信息
    [Column("upload_count")]
    public uint UploadCount { get; set; } = 0;

    [Column("total_download_count")]
    public uint TotalDownloadCount { get; set; } = 0;

    [Column("total_like_count")]
    public uint TotalLikeCount { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // 导航属性
    [ForeignKey(nameof(LevelCode))]
    public virtual UserLevelConfigModel? LevelConfig { get; set; }
}