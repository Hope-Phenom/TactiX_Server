using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 战术文件版本表模型
/// </summary>
[Table("tactics_file_version")]
public class TacticsFileVersionModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("file_id")]
    public long FileId { get; set; }

    [Column("version_number")]
    public uint VersionNumber { get; set; }

    /// <summary>tactix文件内的版本号</summary>
    [Column("tac_version")]
    public uint TacVersion { get; set; }

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

    [Column("changelog")]
    public string? Changelog { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // 导航属性
    public virtual TacticsFileModel? File { get; set; }
}
