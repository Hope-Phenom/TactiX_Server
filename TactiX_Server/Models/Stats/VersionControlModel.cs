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
        public required string Version { get; set; }
        /// <summary>
        /// 是否被禁用
        /// </summary>
        public bool Banned { get; set; }
        /// <summary>
        /// 是否需要强制升级
        /// </summary>
        public bool Force_Upgrade { get; set; }
        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime Release_Time { get; set; }
        /// <summary>
        /// 发布地址
        /// </summary>
        public required string Release_Url { get; set; }
    }
}
