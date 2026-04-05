using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TactiX_Server.Models.Config;
using TactiX_Server.Utils;

namespace TactiX_Server.Services;

/// <summary>
/// 文件验证结果
/// </summary>
public class FileValidationResult
{
    /// <summary>是否验证通过</summary>
    public bool IsValid { get; set; }

    /// <summary>验证失败的层级</summary>
    public int? FailedLayer { get; set; }

    /// <summary>错误信息</summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>警告信息（不阻止验证通过）</summary>
    public List<string> Warnings { get; set; } = new();

    /// <summary>文件SHA256哈希</summary>
    public string? FileHash { get; set; }

    /// <summary>检测到的种族</summary>
    public string? DetectedRace { get; set; }

    /// <summary>解析出的名称</summary>
    public string? Name { get; set; }

    /// <summary>解析出的作者</summary>
    public string? Author { get; set; }
}

/// <summary>
/// 文件安全验证器接口
/// </summary>
public interface IFileSecurityValidator
{
    /// <summary>验证文件内容</summary>
    Task<FileValidationResult> ValidateAsync(byte[] content, string fileName);

    /// <summary>验证文件内容（流版本）</summary>
    Task<FileValidationResult> ValidateAsync(Stream content, string fileName);
}

/// <summary>
/// 文件安全验证器实现
/// 提供5层安全验证：
/// 1. 结构验证 - JSON格式、必需字段
/// 2. 大小验证 - 文件大小、字符串长度、数组长度
/// 3. 内容验证 - 敏感关键词、危险模式
/// 4. XSS验证 - HTML标签、JavaScript代码
/// 5. 重复验证 - 文件哈希（返回hash供后续查重）
/// </summary>
public class FileSecurityValidator : IFileSecurityValidator
{
    private readonly ILogger<FileSecurityValidator> _logger;
    private readonly TacticsSecurityConfig _config;

    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    private static readonly string[] StringProperties = { "name", "author", "description" };

    /// <summary>
    /// 敏感关键词列表
    /// </summary>
    private static readonly string[] SensitiveKeywords =
    {
        "script", "eval", "exec", "system", "cmd", "shell",
        "document.cookie", "window.location", "localStorage",
        "sessionStorage", "XMLHttpRequest", "fetch(",
        "../", "..\\", "file://", "ftp://"
    };

    /// <summary>
    /// XSS危险模式
    /// </summary>
    private static readonly string[] XssPatterns =
    {
        "<script", "</script>", "javascript:", "onerror=",
        "onload=", "onclick=", "onmouseover=", "onfocus=",
        "onblur=", "<iframe", "<object", "<embed",
        "expression(", "vbscript:", "data:text/html"
    };

    public FileSecurityValidator(
        ILogger<FileSecurityValidator> logger,
        IOptions<TacticsHallConfig> config)
    {
        _logger = logger;
        _config = config.Value.Security;
    }

    public async Task<FileValidationResult> ValidateAsync(byte[] content, string fileName)
    {
        var result = new FileValidationResult();

        try
        {
            // 检查文件是否为空
            if (content == null || content.Length == 0)
            {
                result.Errors.Add("文件内容为空");
                result.FailedLayer = 1;
                return result;
            }

            // 计算文件哈希（复用HashUtil）
            result.FileHash = HashUtil.ComputeSha256(content);

            // 解析JSON（只解析一次）
            JsonDocument jsonDoc;
            JsonElement root;
            try
            {
                var jsonContent = Encoding.UTF8.GetString(content);
                jsonDoc = JsonDocument.Parse(jsonContent);
                root = jsonDoc.RootElement;
            }
            catch (JsonException ex)
            {
                result.Errors.Add($"JSON格式无效: {ex.Message}");
                result.FailedLayer = 1;
                return result;
            }
            catch (DecoderFallbackException ex)
            {
                result.Errors.Add($"文件编码无效，应为UTF-8: {ex.Message}");
                result.FailedLayer = 1;
                return result;
            }

            using (jsonDoc)
            {
                // 第1层：结构验证
                if (!ValidateStructure(root, result))
                {
                    result.FailedLayer = 1;
                    return result;
                }

                // 第2层：大小验证
                if (!ValidateSize(content.Length, root, result))
                {
                    result.FailedLayer = 2;
                    return result;
                }

                // 第3层：内容验证
                var jsonContentForValidation = Encoding.UTF8.GetString(content);
                if (!ValidateContent(jsonContentForValidation, result))
                {
                    result.FailedLayer = 3;
                    return result;
                }

                // 第4层：XSS验证
                if (_config.EnableXssFilter && !ValidateXss(jsonContentForValidation, result))
                {
                    result.FailedLayer = 4;
                    return result;
                }

                // 第5层：重复验证（返回哈希供外部查重）
                // 注：实际查重逻辑需要数据库支持，在M4实现

                // 提取元数据
                ExtractMetadata(root, result);
            }

            result.IsValid = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件验证过程中发生异常: {FileName}", fileName);
            result.Errors.Add($"验证过程发生异常: {ex.Message}");
            result.FailedLayer = 0;
            return result;
        }
    }

    public async Task<FileValidationResult> ValidateAsync(Stream content, string fileName)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms);
        return await ValidateAsync(ms.ToArray(), fileName);
    }

    /// <summary>
    /// 第1层：结构验证
    /// </summary>
    private bool ValidateStructure(JsonElement root, FileValidationResult result)
    {
        // 必须是对象
        if (root.ValueKind != JsonValueKind.Object)
        {
            result.Errors.Add("JSON根元素必须是对象");
            return false;
        }

        // 必须包含actions数组
        if (!root.TryGetProperty("actions", out var actions)
            || actions.ValueKind != JsonValueKind.Array)
        {
            result.Errors.Add("缺少必需的'actions'数组字段");
            return false;
        }

        // 验证actions数组不为空
        if (actions.GetArrayLength() == 0)
        {
            result.Errors.Add("'actions'数组不能为空");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 第2层：大小验证
    /// </summary>
    private bool ValidateSize(int fileSize, JsonElement root, FileValidationResult result)
    {
        // 检查文件大小
        if (fileSize > MaxFileSizeBytes)
        {
            result.Errors.Add($"文件大小({fileSize}字节)超过限制({MaxFileSizeBytes}字节)");
            return false;
        }

        // 检查actions数量
        if (root.TryGetProperty("actions", out var actions))
        {
            var actionCount = actions.GetArrayLength();
            if (actionCount > _config.MaxActionsCount)
            {
                result.Errors.Add($"动作数量({actionCount})超过限制({_config.MaxActionsCount})");
                return false;
            }
        }

        // 检查字符串字段长度
        foreach (var prop in StringProperties)
        {
            if (root.TryGetProperty(prop, out var element)
                && element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                if (value != null && value.Length > _config.MaxStringLength)
                {
                    result.Errors.Add($"字段'{prop}'长度({value.Length})超过限制({_config.MaxStringLength})");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// 第3层：内容验证
    /// </summary>
    private bool ValidateContent(string content, FileValidationResult result)
    {
        var lowerContent = content.ToLowerInvariant();
        var foundKeywords = new List<string>();

        foreach (var keyword in SensitiveKeywords)
        {
            if (lowerContent.Contains(keyword.ToLowerInvariant()))
            {
                foundKeywords.Add(keyword);
            }
        }

        if (foundKeywords.Count > 0)
        {
            // 记录警告但不阻止（某些关键词可能是正常内容）
            result.Warnings.Add($"检测到敏感关键词: {string.Join(", ", foundKeywords.Take(5))}");
            _logger.LogWarning("检测到敏感关键词: {Keywords}", string.Join(", ", foundKeywords));
        }

        // 检查异常模式 - 路径遍历
        if (content.Contains("..") && (content.Contains("/") || content.Contains("\\")))
        {
            result.Errors.Add("检测到可能的路径遍历攻击");
            return false;
        }

        // 检查JSON注入模式
        if (content.Contains("__proto__") || content.Contains("constructor"))
        {
            result.Warnings.Add("检测到可能的原型污染模式");
        }

        return true;
    }

    /// <summary>
    /// 第4层：XSS验证
    /// </summary>
    private bool ValidateXss(string content, FileValidationResult result)
    {
        var lowerContent = content.ToLowerInvariant();
        var foundPatterns = new List<string>();

        foreach (var pattern in XssPatterns)
        {
            if (lowerContent.Contains(pattern.ToLowerInvariant()))
            {
                foundPatterns.Add(pattern);
            }
        }

        if (foundPatterns.Count > 0)
        {
            result.Errors.Add($"检测到XSS攻击模式: {string.Join(", ", foundPatterns.Take(5))}");
            _logger.LogWarning("检测到XSS攻击模式: {Patterns}", string.Join(", ", foundPatterns));
            return false;
        }

        return true;
    }

    /// <summary>
    /// 提取文件元数据
    /// </summary>
    private void ExtractMetadata(JsonElement root, FileValidationResult result)
    {
        if (root.TryGetProperty("name", out var nameElement))
        {
            result.Name = nameElement.GetString();
        }

        if (root.TryGetProperty("author", out var authorElement))
        {
            result.Author = authorElement.GetString();
        }

        // 检测种族（复用TacticsFileParser）
        if (root.TryGetProperty("actions", out var actions)
            && actions.ValueKind == JsonValueKind.Array)
        {
            result.DetectedRace = TacticsFileParser.DetectRaceFromActions(actions);
        }
    }
}