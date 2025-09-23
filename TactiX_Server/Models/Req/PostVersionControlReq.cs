namespace TactiX_Server.Models.Req
{
    /// <summary>
    /// 版本控制请求数据
    /// </summary>
    [Serializable]
    public class PostVersionControlReq
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public required string Version { get; set; }
    }
}
