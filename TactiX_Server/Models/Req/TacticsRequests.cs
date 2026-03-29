namespace TactiX_Server.Models.Req;

/// <summary>
/// 上传战术文件请求
/// </summary>
public class UploadTacticsRequest
{
    /// <summary>
    /// 版本更新说明
    /// </summary>
    public string? Changelog { get; set; }
}

/// <summary>
/// 上传新版本请求
/// </summary>
public class UploadVersionRequest
{
    /// <summary>
    /// 配装码
    /// </summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// 版本更新说明
    /// </summary>
    public string? Changelog { get; set; }
}

/// <summary>
/// 搜索战术文件请求
/// </summary>
public class SearchTacticsRequest
{
    /// <summary>
    /// 关键词
    /// </summary>
    public string? Keyword { get; set; }

    /// <summary>
    /// 使用种族 (P/T/Z)
    /// </summary>
    public string? RacePlayed { get; set; }

    /// <summary>
    /// 对手种族 (P/T/Z)
    /// </summary>
    public string? RaceOpponent { get; set; }

    /// <summary>
    /// 对抗类型 (PvT/PvZ等)
    /// </summary>
    public string? Matchup { get; set; }

    /// <summary>
    /// 战术风格类型
    /// </summary>
    public int? TacticType { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; } = 20;

    /// <summary>
    /// 排序方式
    /// </summary>
    public string SortBy { get; set; } = "newest"; // newest, popular, downloads
}
