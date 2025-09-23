namespace TactiX_Server.Models.Resp
{
    /// <summary>
    /// 版本控制响应数据
    /// </summary>
    [Serializable]
    public class PostVersionControlResp
    {
        /// <summary>
        /// 最新的版本
        /// </summary>
        public required string LastestVersion { get; set; }
        /// <summary>
        /// 版本是否被禁用
        /// </summary>
        public bool Banned { get; set; }
        /// <summary>
        /// 是否要进行强制升级
        /// </summary>
        public bool Force_Upgrade { get; set; }
        /// <summary>
        /// 发布地址
        /// </summary>
        public required string Release_Url { get; set; }
    }
}
