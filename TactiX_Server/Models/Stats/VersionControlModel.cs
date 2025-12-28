using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Stats
{
    [Table("stats_version_control")]
    public class VersionControlModel
    {
        /// <summary>
        /// 主索引
        /// </summary>
        [Key]
        public uint Id { get; set; }
        /// <summary>
        /// 版本号
        /// </summary>
        [Column("version")]
        public required string Version { get; set; }
        /// <summary>
        /// 是否被禁用
        /// </summary>
        [Column("banned")]
        public bool Banned { get; set; }
        /// <summary>
        /// 是否需要强制升级
        /// </summary>
        [Column("force_upgrade")]
        public bool ForceUpgrade { get; set; }
        /// <summary>
        /// 发布时间
        /// </summary>
        [Column("release_time")]
        public DateTime ReleaseTime { get; set; }
        /// <summary>
        /// 发布地址
        /// </summary>
        [Column("release_url")]
        public required string ReleaseUrl { get; set; }
    }
}
