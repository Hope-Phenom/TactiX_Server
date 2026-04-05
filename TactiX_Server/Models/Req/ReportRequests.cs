using System.ComponentModel.DataAnnotations;

namespace TactiX_Server.Models.Req;

/// <summary>
/// 提交举报请求
/// </summary>
public class SubmitReportRequest
{
    /// <summary>配装码</summary>
    [Required]
    [StringLength(8, MinimumLength = 8)]
    public string ShareCode { get; set; } = string.Empty;

    /// <summary>举报原因类型</summary>
    [Required]
    [StringLength(32)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>举报详细描述</summary>
    [StringLength(500)]
    public string? Description { get; set; }
}

/// <summary>
/// 处理举报请求（管理员）
/// </summary>
public class ProcessReportRequest
{
    /// <summary>是否采取行动（删除文件）</summary>
    public bool TakeAction { get; set; }

    /// <summary>处理结果说明</summary>
    [StringLength(500)]
    public string? HandleResult { get; set; }
}

/// <summary>
/// 获取举报列表请求
/// </summary>
public class GetReportsRequest
{
    /// <summary>状态筛选: pending/processed/ignored</summary>
    [StringLength(16)]
    public string? Status { get; set; }

    /// <summary>页码</summary>
    [Range(1, 1000)]
    public int Page { get; set; } = 1;

    /// <summary>每页数量</summary>
    [Range(1, 50)]
    public int PageSize { get; set; } = 20;
}