using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Models.Resp;

/// <summary>
/// 上传战术文件响应
/// </summary>
public class UploadTacticsResponse
{
    /// <summary>
    /// 配装码
    /// </summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// 文件ID
    /// </summary>
    public long FileId { get; set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// 审核状态
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// 提示消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 战术文件详情响应
/// </summary>
public class TacticsDetailResponse
{
    /// <summary>
    /// 配装码
    /// </summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// 战术名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 战术描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作者名称
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// 上传者信息
    /// </summary>
    public UserBriefResponse? Uploader { get; set; }

    /// <summary>
    /// 使用种族
    /// </summary>
    public string RacePlayed { get; set; } = string.Empty;

    /// <summary>
    /// 对手种族
    /// </summary>
    public string RaceOpponent { get; set; } = string.Empty;

    /// <summary>
    /// 对抗类型
    /// </summary>
    public string Matchup { get; set; } = string.Empty;

    /// <summary>
    /// 战术风格类型
    /// </summary>
    public int TacticType { get; set; }

    /// <summary>
    /// 游戏模组
    /// </summary>
    public string ModName { get; set; } = string.Empty;

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// 点赞数
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// 当前版本号
    /// </summary>
    public int CurrentVersion { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 用户简要信息响应
/// </summary>
public class UserBriefResponse
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    public string Nickname { get; set; } = string.Empty;

    /// <summary>
    /// 头像URL
    /// </summary>
    public string AvatarUrl { get; set; } = string.Empty;

    /// <summary>
    /// 用户等级
    /// </summary>
    public string LevelCode { get; set; } = string.Empty;
}

/// <summary>
/// 战术文件列表项响应
/// </summary>
public class TacticsListItemResponse
{
    /// <summary>
    /// 配装码
    /// </summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>
    /// 战术名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 作者名称
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// 使用种族
    /// </summary>
    public string RacePlayed { get; set; } = string.Empty;

    /// <summary>
    /// 对抗类型
    /// </summary>
    public string Matchup { get; set; } = string.Empty;

    /// <summary>
    /// 战术风格类型
    /// </summary>
    public int TacticType { get; set; }

    /// <summary>
    /// 下载次数
    /// </summary>
    public int DownloadCount { get; set; }

    /// <summary>
    /// 点赞数
    /// </summary>
    public int LikeCount { get; set; }

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 搜索结果分页响应
/// </summary>
public class PagedTacticsResponse
{
    /// <summary>
    /// 数据列表
    /// </summary>
    public List<TacticsListItemResponse> Items { get; set; } = new();

    /// <summary>
    /// 总数量
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 当前页码
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每页数量
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
}

/// <summary>
/// 版本信息响应
/// </summary>
public class VersionInfoResponse
{
    /// <summary>
    /// 版本号
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// 战术文件版本号
    /// </summary>
    public int TacVersion { get; set; }

    /// <summary>
    /// 更新说明
    /// </summary>
    public string Changelog { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
