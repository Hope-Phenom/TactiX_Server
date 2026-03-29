namespace TactiX_Server.Utils;

/// <summary>
/// 配装码工具 - 62进制编码
/// </summary>
public static class ShareCodeUtil
{
    private const string Base62Chars = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int CodeLength = 8;

    /// <summary>
    /// 将数据库ID转换为8位配装码
    /// </summary>
    public static string ToShareCode(long id)
    {
        if (id <= 0)
            throw new ArgumentException("ID must be positive", nameof(id));

        var result = new char[CodeLength];
        for (int i = CodeLength - 1; i >= 0; i--)
        {
            result[i] = Base62Chars[(int)(id % 62)];
            id /= 62;
        }

        return new string(result);
    }

    /// <summary>
    /// 将配装码解析回数据库ID
    /// </summary>
    public static long FromShareCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != CodeLength)
            throw new ArgumentException($"Invalid share code length, expected {CodeLength}", nameof(code));

        long result = 0;
        foreach (char c in code)
        {
            int index = Base62Chars.IndexOf(c);
            if (index < 0)
                throw new ArgumentException($"Invalid character in share code: {c}", nameof(code));

            result = result * 62 + index;
        }

        return result;
    }

    /// <summary>
    /// 生成随机配装码（用于测试）
    /// </summary>
    public static string GenerateRandomCode()
    {
        var random = new Random();
        var result = new char[CodeLength];

        for (int i = 0; i < CodeLength; i++)
        {
            result[i] = Base62Chars[random.Next(Base62Chars.Length)];
        }

        return new string(result);
    }

    /// <summary>
    /// 验证配装码格式是否有效
    /// </summary>
    public static bool IsValidShareCode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != CodeLength)
            return false;

        return code.All(c => Base62Chars.Contains(c));
    }
}
