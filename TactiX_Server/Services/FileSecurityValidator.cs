using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Options;

using TactiX_Server.Models.Config;
using TactiX_Server.Utils;

namespace TactiX_Server.Services;

/// <summary>
/// 文件安全验证接口
/// </summary>
public interface IFileSecurityValidator
{
    /// <summary>
    /// 执行完整的5层安全验证
    /// </summary>
    Task<FileValidationResult> ValidateAsync(string filePath, string originalFileName);
}

/// <summary>
/// 文件安全验证器 - 5层安全验证
/// </summary>
public class FileSecurityValidator : IFileSecurityValidator
{
    private readonly TacticsHallConfig _config;
    private readonly ILogger<FileSecurityValidator> _logger;

    public FileSecurityValidator(
        IOptions<TacticsHallConfig> config,
        ILogger<FileSecurityValidator> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<FileValidationResult> ValidateAsync(string filePath, string originalFileName)
    {
        var errors = new List<string>();

        try
        {
            // 第1层: 基础验证
            var basicResult = ValidateBasic(filePath, originalFileName);
            if (!basicResult.IsValid)
            {
                errors.AddRange(basicResult.Errors);
                _logger.LogWarning("文件基础验证失败: {Errors}", string.Join(", ", basicResult.Errors));
                return FileValidationResult.Fail(errors, FileValidationErrorType.Basic);
            }

            // 读取文件内容
            var fileContent = await File.ReadAllTextAsync(filePath);

            // 第2层: 格式验证
            var formatResult = ValidateFormat(fileContent);
            if (!formatResult.IsValid)
            {
                errors.AddRange(formatResult.Errors);
                _logger.LogWarning("文件格式验证失败: {Errors}", string.Join(", ", formatResult.Errors));
                return FileValidationResult.Fail(errors, FileValidationErrorType.Format);
            }

            // 第3层: 内容深度验证
            var contentResult = ValidateContent(fileContent);
            if (!contentResult.IsValid)
            {
                errors.AddRange(contentResult.Errors);
                _logger.LogWarning("文件内容验证失败: {Errors}", string.Join(", ", contentResult.Errors));
                return FileValidationResult.Fail(errors, FileValidationErrorType.Content);
            }

            // 第4层: 安全验证
            var securityResult = ValidateSecurity(fileContent);
            if (!securityResult.IsValid)
            {
                errors.AddRange(securityResult.Errors);
                _logger.LogWarning("文件安全验证失败: {Errors}", string.Join(", ", securityResult.Errors));
                return FileValidationResult.Fail(errors, FileValidationErrorType.Security);
            }

            // 第5层: 哈希校验
            var hashResult = await ValidateHashAsync(filePath);
            if (!hashResult.IsValid)
            {
                errors.AddRange(hashResult.Errors);
                _logger.LogWarning("文件哈希验证失败: {Errors}", string.Join(", ", hashResult.Errors));
                return FileValidationResult.Fail(errors, FileValidationErrorType.Hash);
            }

            // 解析文件以提取信息
            var parsedFile = TacticsFileParser.Parse(fileContent);

            _logger.LogInformation("文件验证通过: {FileName}", originalFileName);

            return FileValidationResult.Success(parsedFile, hashResult.Hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件验证过程中发生异常: {FileName}", originalFileName);
            errors.Add($"验证异常: {ex.Message}");
            return FileValidationResult.Fail(errors, FileValidationErrorType.Unknown);
        }
    }

    /// <summary>
    /// 第1层: 基础验证
    /// </summary>
    private ValidationStepResult ValidateBasic(string filePath, string originalFileName)
    {
        var errors = new List<string>();

        // 检查文件扩展名
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!_config.AllowedExtensions.Contains(extension))
        {
            errors.Add($"不支持的文件扩展名: {extension}，只支持: {string.Join(", ", _config.AllowedExtensions)}");
        }

        // 检查文件大小
        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length > _config.MaxFileSize)
        {
            errors.Add($"文件大小 {fileInfo.Length} 字节超过限制 {_config.MaxFileSize} 字节");
        }

        // MIME类型检查 (简化检查，实际生产环境可能需要更严格的检查)
        // 这里我们假设.tactix文件是JSON格式
        var mimeType = GetMimeType(filePath);
        if (mimeType != "application/json" && mimeType != "text/plain")
        {
            _logger.LogWarning("文件MIME类型可能不正确: {MimeType}", mimeType);
            // 不阻止，只记录警告
        }

        return new ValidationStepResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 第2层: 格式验证
    /// </summary>
    private ValidationStepResult ValidateFormat(string fileContent)
    {
        return TacticsFileParser.Validate(fileContent) switch
        {
            { IsValid: true } => new ValidationStepResult(true, new List<string>()),
            var result => new ValidationStepResult(false, result.Errors)
        };
    }

    /// <summary>
    /// 第3层: 内容深度验证
    /// </summary>
    private ValidationStepResult ValidateContent(string fileContent)
    {
        var errors = new List<string>();

        try
        {
            var file = TacticsFileParser.Parse(fileContent);

            // 检查Actions数量
            if (file.Actions.Count > _config.Security.MaxActionsCount)
            {
                errors.Add($"Actions数量 {file.Actions.Count} 超过限制 {_config.Security.MaxActionsCount}");
            }

            // 验证每个Action的时间格式合理性
            foreach (var action in file.Actions)
            {
                if (!IsValidTimeRange(action.Time))
                {
                    errors.Add($"不合理的时间值: {action.Time}");
                }
            }

            // 验证战术名称和作者名称长度
            if (file.Name?.Length > _config.Security.MaxStringLength)
            {
                errors.Add($"战术名称长度 {file.Name.Length} 超过限制 {_config.Security.MaxStringLength}");
            }

            if (file.Author?.Length > _config.Security.MaxStringLength)
            {
                errors.Add($"作者名称长度 {file.Author.Length} 超过限制 {_config.Security.MaxStringLength}");
            }

            if (file.Description?.Length > _config.Security.MaxStringLength * 5)
            {
                errors.Add($"描述长度 {file.Description.Length} 超过限制 {_config.Security.MaxStringLength * 5}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"内容解析失败: {ex.Message}");
        }

        return new ValidationStepResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 第4层: 安全验证
    /// </summary>
    private ValidationStepResult ValidateSecurity(string fileContent)
    {
        var errors = new List<string>();

        // XSS防护 - 检查特殊字符
        if (_config.Security.EnableXssFilter)
        {
            // 检查潜在的XSS攻击向量
            var xssPatterns = new[]
            {
                @"<script[^>]*>.*?</script>",
                @"javascript:",
                @"on\w+\s*=",
                @"<iframe",
                @"<object",
                @"<embed"
            };

            foreach (var pattern in xssPatterns)
            {
                if (Regex.IsMatch(fileContent, pattern, RegexOptions.IgnoreCase))
                {
                    errors.Add($"检测到潜在XSS攻击向量: {pattern}");
                }
            }
        }

        // Base64/Unicode隐藏代码检测
        if (ContainsHiddenCode(fileContent))
        {
            errors.Add("检测到潜在的隐藏代码");
        }

        // 控制字符检测
        if (ContainsControlCharacters(fileContent))
        {
            errors.Add("检测到非法控制字符");
        }

        return new ValidationStepResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 第5层: 哈希校验
    /// </summary>
    private async Task<HashValidationResult> ValidateHashAsync(string filePath)
    {
        try
        {
            var hash = await HashUtil.ComputeFileHashAsync(filePath);
            return new HashValidationResult(true, new List<string>(), hash);
        }
        catch (Exception ex)
        {
            return new HashValidationResult(false, new List<string> { $"哈希计算失败: {ex.Message}" }, string.Empty);
        }
    }

    /// <summary>
    /// 获取文件MIME类型
    /// </summary>
    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".json" => "application/json",
            ".txt" => "text/plain",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// 验证时间范围合理性
    /// </summary>
    private static bool IsValidTimeRange(string time)
    {
        try
        {
            var parts = time.Split(':');
            if (parts.Length == 2) // MM:SS
            {
                var minutes = int.Parse(parts[0]);
                var seconds = int.Parse(parts[1]);
                return minutes >= 0 && minutes < 180 && seconds >= 0 && seconds < 60; // 最长3小时
            }
            if (parts.Length == 3) // H:MM:SS
            {
                var hours = int.Parse(parts[0]);
                var minutes = int.Parse(parts[1]);
                var seconds = int.Parse(parts[2]);
                return hours >= 0 && hours < 10 && minutes >= 0 && minutes < 60 && seconds >= 0 && seconds < 60;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 检测隐藏代码
    /// </summary>
    private static bool ContainsHiddenCode(string content)
    {
        // 检测零宽字符
        var zeroWidthChars = new[] { '\u200B', '\u200C', '\u200D', '\uFEFF' };
        if (zeroWidthChars.Any(c => content.Contains(c)))
            return true;

        // 检测异常的Unicode序列
        // 这里可以添加更多检测逻辑

        return false;
    }

    /// <summary>
    /// 检测控制字符
    /// </summary>
    private static bool ContainsControlCharacters(string content)
    {
        // 允许正常空白字符 (0x09, 0x0A, 0x0D)，其他控制字符禁止
        foreach (char c in content)
        {
            if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
                return true;
        }
        return false;
    }
}

/// <summary>
/// 文件验证结果
/// </summary>
public class FileValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public FileValidationErrorType ErrorType { get; set; }
    public TactixFile? ParsedFile { get; set; }
    public string FileHash { get; set; } = string.Empty;

    public static FileValidationResult Success(TactixFile parsedFile, string hash) => new()
    {
        IsValid = true,
        ParsedFile = parsedFile,
        FileHash = hash
    };

    public static FileValidationResult Fail(List<string> errors, FileValidationErrorType errorType) => new()
    {
        IsValid = false,
        Errors = errors,
        ErrorType = errorType
    };
}

/// <summary>
/// 验证错误类型
/// </summary>
public enum FileValidationErrorType
{
    Basic,      // 基础验证失败
    Format,     // 格式验证失败
    Content,    // 内容验证失败
    Security,   // 安全验证失败
    Hash,       // 哈希验证失败
    Unknown     // 未知错误
}

/// <summary>
/// 验证步骤结果
/// </summary>
public class ValidationStepResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }

    public ValidationStepResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }
}

/// <summary>
/// 哈希验证结果
/// </summary>
public class HashValidationResult : ValidationStepResult
{
    public string Hash { get; set; }

    public HashValidationResult(bool isValid, List<string> errors, string hash) : base(isValid, errors)
    {
        Hash = hash;
    }
}
