namespace TactiX_Server.Models.Resp;

/// <summary>
/// 上传战术文件响应
/// </summary>
public class UploadTacticsResponse
{
    /// <summary>配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>文件ID</summary>
    public long FileId { get; set; }

    /// <summary>版本号</summary>
    public int VersionNumber { get; set; }

    /// <summary>审核状态</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 战术文件详情响应
/// </summary>
public class TacticsDetailResponse
{
    /// <summary>配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>战术名称</summary>
    public string? Name { get; set; }

    /// <summary>作者名称</summary>
    public string? Author { get; set; }

    /// <summary>种族代码</summary>
    public string? Race { get; set; }

    /// <summary>种族显示名称</summary>
    public string? RaceDisplay { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long FileSize { get; set; }

    /// <summary>下载次数</summary>
    public uint DownloadCount { get; set; }

    /// <summary>点赞次数</summary>
    public uint LikeCount { get; set; }

    /// <summary>当前版本号</summary>
    public int CurrentVersion { get; set; }

    /// <summary>审核状态</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>上传时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>上传者信息</summary>
    public UserBriefResponse? Uploader { get; set; }
}

/// <summary>
/// 用户简要信息响应
/// </summary>
public class UserBriefResponse
{
    /// <summary>用户ID</summary>
    public long Id { get; set; }

    /// <summary>昵称</summary>
    public string? Nickname { get; set; }

    /// <summary>头像URL</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>等级代码</summary>
    public string? LevelCode { get; set; }

    /// <summary>等级名称</summary>
    public string? LevelName { get; set; }
}

/// <summary>
/// 搜索战术文件响应
/// </summary>
public class SearchTacticsResponse
{
    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>文件列表</summary>
    public List<TacticsBriefResponse> Files { get; set; } = new();
}

/// <summary>
/// 战术文件简要信息响应
/// </summary>
public class TacticsBriefResponse
{
    /// <summary>配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>战术名称</summary>
    public string? Name { get; set; }

    /// <summary>作者名称</summary>
    public string? Author { get; set; }

    /// <summary>种族代码</summary>
    public string? Race { get; set; }

    /// <summary>种族显示名称</summary>
    public string? RaceDisplay { get; set; }

    /// <summary>下载次数</summary>
    public uint DownloadCount { get; set; }

    /// <summary>点赞次数</summary>
    public uint LikeCount { get; set; }

    /// <summary>当前版本号</summary>
    public int CurrentVersion { get; set; }

    /// <summary>上传时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>上传者昵称</summary>
    public string? UploaderNickname { get; set; }
}

/// <summary>
/// 文件版本列表响应
/// </summary>
public class TacticsVersionListResponse
{
    /// <summary>配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>版本列表</summary>
    public List<TacticsVersionResponse> Versions { get; set; } = new();
}

/// <summary>
/// 文件版本信息响应
/// </summary>
public class TacticsVersionResponse
{
    /// <summary>版本号</summary>
    public int VersionNumber { get; set; }

    /// <summary>文件大小</summary>
    public long FileSize { get; set; }

    /// <summary>版本更新说明</summary>
    public string? Changelog { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; set; }
}