namespace TactiX_Server.Models.Resp;

/// <summary>
/// 提交举报响应
/// </summary>
public class SubmitReportResponse
{
    /// <summary>举报ID</summary>
    public long ReportId { get; set; }

    /// <summary>消息</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 举报列表响应
/// </summary>
public class ReportListResponse
{
    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>举报列表</summary>
    public List<ReportItemResponse> Reports { get; set; } = new();
}

/// <summary>
/// 举报项响应
/// </summary>
public class ReportItemResponse
{
    /// <summary>举报ID</summary>
    public long Id { get; set; }

    /// <summary>被举报文件配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>被举报文件名称</summary>
    public string? FileName { get; set; }

    /// <summary>举报原因类型</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>举报原因显示名称</summary>
    public string ReasonDisplay { get; set; } = string.Empty;

    /// <summary>举报描述</summary>
    public string? Description { get; set; }

    /// <summary>处理状态</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>处理状态显示名称</summary>
    public string StatusDisplay { get; set; } = string.Empty;

    /// <summary>举报者昵称</summary>
    public string? ReporterNickname { get; set; }

    /// <summary>举报时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>处理时间</summary>
    public DateTime? ProcessedAt { get; set; }
}

/// <summary>
/// 举报详情响应
/// </summary>
public class ReportDetailResponse
{
    /// <summary>举报ID</summary>
    public long Id { get; set; }

    /// <summary>被举报文件信息</summary>
    public ReportFileResponse? File { get; set; }

    /// <summary>举报原因类型</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>举报原因显示名称</summary>
    public string ReasonDisplay { get; set; } = string.Empty;

    /// <summary>举报描述</summary>
    public string? Description { get; set; }

    /// <summary>处理状态</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>处理状态显示名称</summary>
    public string StatusDisplay { get; set; } = string.Empty;

    /// <summary>举报者信息</summary>
    public UserBriefResponse? Reporter { get; set; }

    /// <summary>处理管理员信息</summary>
    public UserBriefResponse? Handler { get; set; }

    /// <summary>处理结果说明</summary>
    public string? HandleResult { get; set; }

    /// <summary>举报时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>更新时间</summary>
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// 被举报文件简要信息
/// </summary>
public class ReportFileResponse
{
    /// <summary>配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>文件名称</summary>
    public string? Name { get; set; }

    /// <summary>作者</summary>
    public string? Author { get; set; }

    /// <summary>种族</summary>
    public string? Race { get; set; }

    /// <summary>上传者昵称</summary>
    public string? UploaderNickname { get; set; }
}

/// <summary>
/// 举报统计响应
/// </summary>
public class ReportStatsResponse
{
    /// <summary>待处理数量</summary>
    public int PendingCount { get; set; }

    /// <summary>已处理数量</summary>
    public int ProcessedCount { get; set; }

    /// <summary>已忽略数量</summary>
    public int IgnoredCount { get; set; }

    /// <summary>总数</summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// 用户举报历史响应
/// </summary>
public class UserReportListResponse
{
    /// <summary>总数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页</summary>
    public int Page { get; set; }

    /// <summary>每页数量</summary>
    public int PageSize { get; set; }

    /// <summary>举报列表</summary>
    public List<UserReportItemResponse> Reports { get; set; } = new();
}

/// <summary>
/// 用户举报项响应
/// </summary>
public class UserReportItemResponse
{
    /// <summary>举报ID</summary>
    public long Id { get; set; }

    /// <summary>被举报文件配装码</summary>
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>被举报文件名称</summary>
    public string? FileName { get; set; }

    /// <summary>举报原因显示名称</summary>
    public string ReasonDisplay { get; set; } = string.Empty;

    /// <summary>处理状态显示名称</summary>
    public string StatusDisplay { get; set; } = string.Empty;

    /// <summary>举报时间</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>处理结果说明</summary>
    public string? HandleResult { get; set; }
}