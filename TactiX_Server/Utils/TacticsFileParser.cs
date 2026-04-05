using System.Text.Json;

namespace TactiX_Server.Utils;

/// <summary>
/// 战术文件解析结果
/// </summary>
public class TacticsParseResult
{
    /// <summary>是否解析成功</summary>
    public bool Success { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>战术名称</summary>
    public string? Name { get; set; }

    /// <summary>作者名称</summary>
    public string? Author { get; set; }

    /// <summary>种族代码: P/T/Z</summary>
    public string? Race { get; set; }

    /// <summary>原始JSON内容（用于后续验证）</summary>
    public JsonDocument? RawDocument { get; set; }
}

/// <summary>
/// 战术文件解析器
/// 支持解析.tactix JSON文件并提取元数据
/// </summary>
public class TacticsFileParser
{
    /// <summary>
    /// 解析战术文件JSON内容
    /// </summary>
    /// <param name="jsonContent">JSON字符串内容</param>
    /// <returns>解析结果</returns>
    public TacticsParseResult Parse(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            return new TacticsParseResult
            {
                Success = false,
                ErrorMessage = "文件内容为空"
            };
        }

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            // 提取基本信息
            string? name = null;
            string? author = null;
            string? race = null;

            if (root.TryGetProperty("name", out var nameElement))
            {
                name = nameElement.GetString();
            }

            if (root.TryGetProperty("author", out var authorElement))
            {
                author = authorElement.GetString();
            }

            // 从actions中识别种族
            if (root.TryGetProperty("actions", out var actionsElement)
                && actionsElement.ValueKind == JsonValueKind.Array)
            {
                race = DetectRaceFromActions(actionsElement);
            }

            return new TacticsParseResult
            {
                Success = true,
                Name = name,
                Author = author,
                Race = race,
                RawDocument = jsonDoc
            };
        }
        catch (JsonException ex)
        {
            return new TacticsParseResult
            {
                Success = false,
                ErrorMessage = $"JSON解析失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 解析战术文件JSON内容（异步版本）
    /// </summary>
    /// <param name="jsonStream">JSON数据流</param>
    /// <returns>解析结果</returns>
    public async Task<TacticsParseResult> ParseAsync(Stream jsonStream)
    {
        try
        {
            var jsonDoc = await JsonDocument.ParseAsync(jsonStream);
            var root = jsonDoc.RootElement;

            string? name = null;
            string? author = null;
            string? race = null;

            if (root.TryGetProperty("name", out var nameElement))
            {
                name = nameElement.GetString();
            }

            if (root.TryGetProperty("author", out var authorElement))
            {
                author = authorElement.GetString();
            }

            if (root.TryGetProperty("actions", out var actionsElement)
                && actionsElement.ValueKind == JsonValueKind.Array)
            {
                race = DetectRaceFromActions(actionsElement);
            }

            return new TacticsParseResult
            {
                Success = true,
                Name = name,
                Author = author,
                Race = race,
                RawDocument = jsonDoc
            };
        }
        catch (JsonException ex)
        {
            return new TacticsParseResult
            {
                Success = false,
                ErrorMessage = $"JSON解析失败: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// 从actions数组中检测种族
    /// 识别规则：取第一个Action.ItemAbbr的前缀字符
    /// - 'P' → 神族(Protoss)
    /// - 'T' → 人族(Terran)
    /// - 'Z' → 虫族(Zerg)
    /// </summary>
    public static string? DetectRaceFromActions(JsonElement actionsElement)
    {
        foreach (var action in actionsElement.EnumerateArray())
        {
            if (action.TryGetProperty("itemAbbr", out var itemAbbrElement))
            {
                var itemAbbr = itemAbbrElement.GetString();
                if (!string.IsNullOrEmpty(itemAbbr))
                {
                    var prefix = char.ToUpperInvariant(itemAbbr[0]);
                    return prefix switch
                    {
                        'P' => "P", // Protoss
                        'T' => "T", // Terran
                        'Z' => "Z", // Zerg
                        _ => null
                    };
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 验证JSON结构是否符合基本要求
    /// </summary>
    public bool ValidateBasicStructure(JsonDocument document)
    {
        var root = document.RootElement;

        // 必须是对象
        if (root.ValueKind != JsonValueKind.Object)
            return false;

        // 必须有actions数组
        if (!root.TryGetProperty("actions", out var actions)
            || actions.ValueKind != JsonValueKind.Array)
            return false;

        return true;
    }
}