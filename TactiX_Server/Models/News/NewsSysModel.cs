using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.News
{
    [Table("news_sys")]
    public class NewsSysModel
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public int Id { get; set; }
        /// <summary>
        /// 标题
        /// </summary>
        [Column("title")]
        public string Title { get; set; } = string.Empty;
        /// <summary>
        /// 链接
        /// </summary>
        [Column("link")]
        public string Link { get; set; } = string.Empty;
        /// <summary>
        /// 更新日期
        /// </summary>
        [Column("datetime")]
        public DateTime DateTime { get; set; }
    }
}
