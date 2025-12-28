using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TactiX_Server.Models.Stats
{
    /// <summary>
    /// 异常上报模型
    /// </summary>
    [Table("stats_excption_report")]
    public class ExceptionReportModel
    {
        /// <summary>
        /// 主索引
        /// </summary>
        [Key]
        public uint Id { get; set; }
        /// <summary>
        /// 错误代码
        /// </summary>
        [Column("error_code")]
        public int Error_Code { get; set; }
        /// <summary>
        /// 错误信息
        /// </summary>
        [Column("error_desc")]
        public required string Error_Desc { get; set; }
        /// <summary>
        /// 用户反馈渠道
        /// </summary>
        [Column("feedback_way")]
        public required string Feedback_Way { get; set; }
        /// <summary>
        /// 用户反馈信息
        /// </summary>
        [Column("feedback_info")]
        public required string Feedback_Info { get; set; }
        /// <summary>
        /// 反馈创建日期
        /// </summary>
        [Column("create_time")]
        public DateTime Create_Time { get; set; }
        /// <summary>
        /// 是否已读，默认False，对应0
        /// </summary>
        [Column("is_read")]
        public bool Is_Read { get; set; }
    }
}
