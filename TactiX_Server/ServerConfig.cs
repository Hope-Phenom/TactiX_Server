namespace TactiX_Server
{
    /// <summary>
    /// 服务器配置对象
    /// </summary>
    public class ServerConfig
    {
        /// <summary>
        /// 论坛爬虫用户名
        /// </summary>
        public string ForumUserName { get; set; } = string.Empty;
        /// <summary>
        /// 论坛爬虫密码
        /// </summary>
        public string ForumPassword { get; set; } = string.Empty;
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public string DBConnString { get; set; } = string.Empty;
    }
}
