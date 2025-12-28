using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.News
{
    /// <summary>
    /// 视频博主的地址配置，用于从中解析要推荐的视频
    /// </summary>
    [Table("config_video_up")]
    public class ConfigVideoUpModel
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        [Column("name")]
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// 类型，0-Bilibili
        /// </summary>
        [Column("type")]
        public int Type { get; set; }
        /// <summary>
        /// 地址
        /// </summary>
        [Column("url")]
        public string Url { get; set; } = string.Empty;
    }
}
