using System.ComponentModel.DataAnnotations;

namespace TactiX_Server.Models.Req;

/// <summary>
/// 上传战术文件请求
/// </summary>
public class UploadTacticsRequest
{
    /// <summary>版本更新说明</summary>
    [StringLength(1000)]
    public string? Changelog { get; set; }
}

/// <summary>
/// 上传战术文件新版本请求
/// </summary>
public class UploadVersionRequest
{
    /// <summary>配装码</summary>
    [Required]
    [StringLength(8, MinimumLength = 8)]
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>版本更新说明</summary>
    [StringLength(1000)]
    public string? Changelog { get; set; }
}

/// <summary>
/// 搜索战术文件请求
/// </summary>
public class SearchTacticsRequest
{
    /// <summary>关键词搜索</summary>
    [StringLength(100)]
    public string? Keyword { get; set; }

    /// <summary>种族筛选: P/T/Z</summary>
    [StringLength(1)]
    public string? Race { get; set; }

    /// <summary>上传者ID</summary>
    public long? UploaderId { get; set; }

    /// <summary>排序方式: latest/popular/downloads</summary>
    [StringLength(16)]
    public string SortBy { get; set; } = "latest";

    /// <summary>页码</summary>
    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    /// <summary>每页数量</summary>
    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 删除战术文件请求
/// </summary>
public class DeleteTacticsRequest
{
    /// <summary>配装码</summary>
    [Required]
    [StringLength(8, MinimumLength = 8)]
    public string ShareCode { get; set; } = string.Empty;
}

/// <summary>
/// 审核请求
/// </summary>
public class ReviewRequest
{
    /// <summary>是否通过审核</summary>
    public bool Approved { get; set; }
}

/// <summary>
/// 批量审核请求
/// </summary>
public class BatchReviewRequest
{
    /// <summary>配装码列表</summary>
    [Required]
    public List<string> ShareCodes { get; set; } = new();

    /// <summary>是否通过审核</summary>
    public bool Approved { get; set; }
}