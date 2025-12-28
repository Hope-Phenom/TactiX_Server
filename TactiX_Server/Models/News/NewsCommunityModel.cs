using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.News
{
    [Table("news_community")]
    public class NewsCommunityModel
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// 更新日期
        /// </summary>
        [Column("update_datetime")]
        public DateTime Update_DateTime { get; set; }
        /// <summary>
        /// Json文本
        /// </summary>
        [Column("json")]
        public string Json {  get; set; } = string.Empty;
        /// <summary>
        /// 类别，0-社区热帖，1-Bilibili视频推荐
        /// </summary>
        [Column("type")]
        public int Type { get; set; }
    }
}
