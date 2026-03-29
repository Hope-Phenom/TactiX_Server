using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 战术文件主表模型
/// </summary>
[Table("tactics_file")]
public class TacticsFileModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>8位配装码</summary>
    [Column("share_code")]
    [Required]
    [StringLength(8)]
    public string ShareCode { get; set; } = string.Empty;

    [Column("uploader_id")]
    public long UploaderId { get; set; }

    [Column("name")]
    [Required]
    [StringLength(128)]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("author_name")]
    [StringLength(64)]
    public string? AuthorName { get; set; }

    [Column("mod_name")]
    [Required]
    [StringLength(32)]
    public string ModName { get; set; } = "StarCraft2";

    [Column("tactic_type")]
    public byte TacticType { get; set; } = 0;

    /// <summary>使用种族: P/T/Z/Unknown</summary>
    [Column("race_played")]
    [StringLength(16)]
    public string? RacePlayed { get; set; }

    /// <summary>对手种族: P/T/Z/Unknown</summary>
    [Column("race_opponent")]
    [StringLength(16)]
    public string? RaceOpponent { get; set; }

    /// <summary>对抗类型，如PvT、PvZ等</summary>
    [Column("matchup")]
    [StringLength(8)]
    public string? Matchup { get; set; }

    [Column("file_path")]
    [Required]
    [StringLength(255)]
    public string FilePath { get; set; } = string.Empty;

    [Column("file_size")]
    public uint FileSize { get; set; }

    /// <summary>文件SHA256哈希值</summary>
    [Column("file_hash")]
    [Required]
    [StringLength(64)]
    public string FileHash { get; set; } = string.Empty;

    [Column("download_count")]
    public uint DownloadCount { get; set; } = 0;

    [Column("like_count")]
    public uint LikeCount { get; set; } = 0;

    /// <summary>审核状态: pending/approved/rejected</summary>
    [Column("status")]
    [Required]
    [StringLength(16)]
    public string Status { get; set; } = "pending";

    [Column("is_latest_version")]
    public bool IsLatestVersion { get; set; } = true;

    [Column("latest_version_id")]
    public long? LatestVersionId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // 导航属性
    public virtual TacticsUserModel? Uploader { get; set; }
    public virtual ICollection<TacticsFileVersionModel>? Versions { get; set; }
}
