namespace TactiX_Server.Models.Config;

/// <summary>
/// 战术大厅配置
/// </summary>
public class TacticsHallConfig
{
    /// <summary>文件存储根路径</summary>
    public string StoragePath { get; set; } = "/app/uploads/tactics";

    /// <summary>临时文件路径</summary>
    public string TempPath { get; set; } = "/app/uploads/temp";

    /// <summary>隔离区路径（存放验证失败的文件）</summary>
    public string QuarantinePath { get; set; } = "/app/uploads/quarantine";

    /// <summary>最大文件大小（字节）</summary>
    public long MaxFileSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>允许的文件扩展名</summary>
    public List<string> AllowedExtensions { get; set; } = new() { ".tactix" };

    /// <summary>安全配置</summary>
    public TacticsSecurityConfig Security { get; set; } = new();
}

/// <summary>
/// 战术文件安全配置
/// </summary>
public class TacticsSecurityConfig
{
    /// <summary>最大字符串长度</summary>
    public int MaxStringLength { get; set; } = 10000;

    /// <summary>最大Actions数量</summary>
    public int MaxActionsCount { get; set; } = 1000;

    /// <summary>是否启用XSS过滤</summary>
    public bool EnableXssFilter { get; set; } = true;

    /// <summary>是否启用重复文件检查</summary>
    public bool EnableDuplicateCheck { get; set; } = true;
}
