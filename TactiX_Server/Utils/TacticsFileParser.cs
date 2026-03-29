using System.Text.Json;

namespace TactiX_Server.Utils;

/// <summary>
/// Tactix文件解析器 - 解析.tactix战术文件
/// </summary>
public class TacticsFileParser
{
    /// <summary>
    /// 解析tactix文件内容
    /// </summary>
    public static TactixFile Parse(string jsonContent)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var file = JsonSerializer.Deserialize<TactixFile>(jsonContent, options)
            ?? throw new InvalidDataException("Failed to parse tactix file");

        // 提取种族信息
        file.RacePlayed = ExtractRaceFromActions(file.Actions);

        return file;
    }

    /// <summary>
    /// 从Actions中提取种族信息
    /// </summary>
    private static string ExtractRaceFromActions(List<TactixAction>? actions)
    {
        if (actions == null || actions.Count == 0)
            return "Unknown";

        foreach (var action in actions)
        {
            if (!string.IsNullOrEmpty(action.ItemAbbr))
            {
                char prefix = char.ToUpper(action.ItemAbbr[0]);
                return prefix switch
                {
                    'P' => "Protoss",
                    'T' => "Terran",
                    'Z' => "Zerg",
                    _ => "Unknown"
                };
            }
        }

        return "Unknown";
    }

    /// <summary>
    /// 验证tactix文件格式
    /// </summary>
    public static ValidationResult Validate(string jsonContent)
    {
        var errors = new List<string>();

        // 检查JSON格式
        TactixFile? file;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            file = JsonSerializer.Deserialize<TactixFile>(jsonContent, options);
            if (file == null)
            {
                errors.Add("JSON解析失败，文件内容为空");
                return new ValidationResult(false, errors);
            }
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON格式错误: {ex.Message}");
            return new ValidationResult(false, errors);
        }

        // 验证必需字段
        if (string.IsNullOrWhiteSpace(file.Id))
            errors.Add("缺少必需字段: Id");

        if (string.IsNullOrWhiteSpace(file.Name))
            errors.Add("缺少必需字段: Name");

        if (string.IsNullOrWhiteSpace(file.Author))
            errors.Add("缺少必需字段: Author");

        if (file.Actions == null || file.Actions.Count == 0)
            errors.Add("Actions数组为空");

        // 验证Actions
        if (file.Actions != null)
        {
            for (int i = 0; i < file.Actions.Count; i++)
            {
                var action = file.Actions[i];
                var actionErrors = ValidateAction(action, i);
                errors.AddRange(actionErrors);
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// 验证单个Action
    /// </summary>
    private static List<string> ValidateAction(TactixAction action, int index)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(action.ItemAbbr))
            errors.Add($"Actions[{index}]: ItemAbbr为空");

        if (string.IsNullOrWhiteSpace(action.Time))
        {
            errors.Add($"Actions[{index}]: Time为空");
        }
        else if (!IsValidTimeFormat(action.Time))
        {
            errors.Add($"Actions[{index}]: Time格式错误，应为 'MM:SS' 或 'M:SS'");
        }

        if (!string.IsNullOrWhiteSpace(action.Supply) && !IsValidSupplyFormat(action.Supply))
        {
            errors.Add($"Actions[{index}]: Supply格式错误，应为 'X/Y'");
        }

        return errors;
    }

    /// <summary>
    /// 验证时间格式
    /// </summary>
    private static bool IsValidTimeFormat(string time)
    {
        // 支持格式: "1:30", "12:45", "01:30", "1:30:45" (MM:SS 或 M:SS 或 H:MM:SS)
        var parts = time.Split(':');
        if (parts.Length < 2 || parts.Length > 3)
            return false;

        foreach (var part in parts)
        {
            if (!int.TryParse(part, out _))
                return false;
        }

        return true;
    }

    /// <summary>
    /// 验证Supply格式
    /// </summary>
    private static bool IsValidSupplyFormat(string supply)
    {
        // 支持格式: "13/15", "200/200"
        var parts = supply.Split('/');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out _) && int.TryParse(parts[1], out _);
    }
}

/// <summary>
/// Tactix文件结构
/// </summary>
public class TactixFile
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ApplicableVersion { get; set; } = "1";
    public int TacticType { get; set; }
    public int TacVersion { get; set; } = 1;
    public string UpdateTime { get; set; } = string.Empty;
    public string ModName { get; set; } = "StarCraft2";
    public int ModVersion { get; set; } = 1;
    public List<TactixAction> Actions { get; set; } = new();

    // 解析出的额外信息
    public string RacePlayed { get; set; } = "Unknown";
}

/// <summary>
/// Tactix动作条目
/// </summary>
public class TactixAction
{
    public string ItemAbbr { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Supply { get; set; } = string.Empty;
    public string? Worker { get; set; }
    public string? Gas { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }

    public ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }
}
