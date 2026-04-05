namespace TactiX_Server.Models.Resp;

/// <summary>
/// 热门战术排行榜响应
/// </summary>
public class LeaderboardResponse
{
    /// <summary>时间周期</summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>种族筛选</summary>
    public string? Race { get; set; }

    /// <summary>排序方式</summary>
    public string SortBy { get; set; } = string.Empty;

    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>战术列表</summary>
    public List<HotFileRankItem> Files { get; set; } = new();
}

/// <summary>
/// 热门战术排行项
/// </summary>
public class HotFileRankItem
{
    /// <summary>排名</summary>
    public int Rank { get; set; }

    /// <summary>配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>战术名称</summary>
    public string? Name { get; set; }

    /// <summary>作者</summary>
    public string? Author { get; set; }

    /// <summary>种族代码</summary>
    public string? Race { get; set; }

    /// <summary>种族显示名称</summary>
    public string? RaceDisplay { get; set; }

    /// <summary>下载次数</summary>
    public uint DownloadCount { get; set; }

    /// <summary>点赞次数</summary>
    public uint LikeCount { get; set; }

    /// <summary>收藏次数</summary>
    public uint FavoriteCount { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>上传者信息</summary>
    public UserBriefResponse? Uploader { get; set; }
}

/// <summary>
/// 贡献者排行榜响应
/// </summary>
public class UploaderLeaderboardResponse
{
    /// <summary>时间周期</summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>贡献者列表</summary>
    public List<UploaderRankItem> Uploaders { get; set; } = new();
}

/// <summary>
/// 贡献者排行项
/// </summary>
public class UploaderRankItem
{
    /// <summary>排名</summary>
    public int Rank { get; set; }

    /// <summary>用户ID</summary>
    public long UserId { get; set; }

    /// <summary>昵称</summary>
    public string? Nickname { get; set; }

    /// <summary>头像URL</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>等级代码</summary>
    public string? LevelCode { get; set; }

    /// <summary>等级名称</summary>
    public string? LevelName { get; set; }

    /// <summary>上传数量</summary>
    public int UploadCount { get; set; }

    /// <summary>总下载次数</summary>
    public uint TotalDownloadCount { get; set; }

    /// <summary>总点赞次数</summary>
    public uint TotalLikeCount { get; set; }

    /// <summary>综合质量评分</summary>
    public double QualityScore { get; set; }
}