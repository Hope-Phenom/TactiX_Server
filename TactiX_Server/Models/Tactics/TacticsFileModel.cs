using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Tactics;

/// <summary>
/// 战术文件模型（M3仅结构，M4正式使用）
/// </summary>
[Table("tactics_file")]
public class TacticsFileModel
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>配装分享码（8字符62进制）</summary>
    [Column("share_code")]
    [Required]
    [StringLength(8)]
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>文件名称（从JSON提取）</summary>
    [Column("name")]
    [StringLength(255)]
    public string? Name { get; set; }

    /// <summary>作者名称（从JSON提取）</summary>
    [Column("author")]
    [StringLength(128)]
    public string? Author { get; set; }

    /// <summary>种族代码: P/T/Z</summary>
    [Column("race")]
    [StringLength(1)]
    public string? Race { get; set; }

    /// <summary>上传用户ID</summary>
    [Column("uploader_id")]
    public long UploaderId { get; set; }

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

    /// <summary>文件版本号</summary>
    [Column("version")]
    public int Version { get; set; } = 1;

    /// <summary>原始文件ID（用于版本追溯）</summary>
    [Column("original_file_id")]
    public long? OriginalFileId { get; set; }

    /// <summary>审核状态: pending/approved/rejected</summary>
    [Column("status")]
    [Required]
    [StringLength(16)]
    public string Status { get; set; } = FileStatus.Pending;

    /// <summary>下载次数</summary>
    [Column("download_count")]
    public uint DownloadCount { get; set; } = 0;

    /// <summary>点赞次数</summary>
    [Column("like_count")]
    public uint LikeCount { get; set; } = 0;

    /// <summary>收藏次数</summary>
    [Column("favorite_count")]
    public uint FavoriteCount { get; set; } = 0;

    /// <summary>最新版本ID</summary>
    [Column("latest_version_id")]
    public long? LatestVersionId { get; set; }

    /// <summary>是否公开</summary>
    [Column("is_public")]
    public bool IsPublic { get; set; } = true;

    /// <summary>是否删除</summary>
    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    // 导航属性
    [ForeignKey(nameof(UploaderId))]
    public virtual TacticsUserModel? Uploader { get; set; }

    [ForeignKey(nameof(OriginalFileId))]
    public virtual TacticsFileModel? OriginalFile { get; set; }

    [ForeignKey(nameof(LatestVersionId))]
    public virtual TacticsFileVersionModel? LatestVersion { get; set; }

    public virtual ICollection<TacticsFileVersionModel> Versions { get; set; } = new List<TacticsFileVersionModel>();
}

/// <summary>
/// 文件状态常量
/// </summary>
public static class FileStatus
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}

/// <summary>
/// 种族常量
/// </summary>
public static class Races
{
    public const string Protoss = "P";
    public const string Terran = "T";
    public const string Zerg = "Z";

    public static readonly string[] All = { Protoss, Terran, Zerg };

    public static bool IsValid(string? race)
    {
        return !string.IsNullOrEmpty(race) && All.Contains(race);
    }

    public static string? GetDisplayName(string? race)
    {
        return race?.ToUpperInvariant() switch
        {
            "P" => "神族",
            "T" => "人族",
            "Z" => "虫族",
            _ => null
        };
    }
}