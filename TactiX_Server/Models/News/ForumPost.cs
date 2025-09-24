namespace TactiX_Server.Models.News
{
    /// <summary>
    /// 论坛爬虫解析条目
    /// </summary>
    public class ForumPost
    {
        /// <summary>
        /// 标题
        /// </summary>
        public required string Title { get; set; }
        /// <summary>
        /// url地址
        /// </summary>
        public required string Url { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public required string Date { get; set; }
    }
}
