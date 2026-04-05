using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 战术文件版本模型
/// </summary>
[Table("tactics_file_version")]
public class TacticsFileVersionModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>关联文件ID</summary>
    [Column("file_id")]
    public long FileId { get; set; }

    /// <summary>版本号（从1开始）</summary>
    [Column("version_number")]
    public int VersionNumber { get; set; }

    /// <summary>文件存储路径</summary>
    [Column("file_path")]
    [Required]
    [StringLength(512)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>文件SHA256哈希</summary>
    [Column("file_hash")]
    [Required]
    [StringLength(64)]
    public string FileHash { get; set; } = string.Empty;

    /// <summary>文件大小（字节）</summary>
    [Column("file_size")]
    public long FileSize { get; set; }

    /// <summary>版本更新说明</summary>
    [Column("changelog")]
    public string? Changelog { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // 导航属性
    [ForeignKey(nameof(FileId))]
    public virtual TacticsFileModel? File { get; set; }
}