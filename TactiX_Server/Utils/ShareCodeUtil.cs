namespace TactiX_Server.Utils;

/// <summary>
/// 62进制配装码工具
/// 用于生成和解析8字符的配装分享码
/// 字符集: 0-9 (10) + A-Z (26) + a-z (26) = 62个字符
/// </summary>
public static class ShareCodeUtil
{
    /// <summary>
    /// 62进制字符集
    /// 索引0-9: '0'-'9'
    /// 索引10-35: 'A'-'Z'
    /// 索引36-61: 'a'-'z'
    /// </summary>
    private const string Charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    private const int Base = 62;
    private const int CodeLength = 8;

    /// <summary>
    /// 将ID编码为8字符配装码
    /// 编码规则: 编码值 = ID - 1，使ID从1开始时编码从全0开始
    /// </summary>
    /// <param name="id">文件ID（必须大于0）</param>
    /// <returns>8字符配装码，如果ID无效返回null</returns>
    /// <example>
    /// ID=1 → "00000000"
    /// ID=62 → "0000000z"
    /// ID=63 → "00000010"
    /// </example>
    public static string? Encode(long id)
    {
        if (id <= 0) return null;

        // ID从1开始，编码时减1使"00000000"对应ID=1
        var value = id - 1;

        var chars = new char[CodeLength];

        // 从右到左填充字符
        for (var i = CodeLength - 1; i >= 0; i--)
        {
            chars[i] = Charset[(int)(value % Base)];
            value /= Base;
        }

        // 如果还有剩余，说明ID超出了8字符可表示的范围
        if (value > 0) return null;

        return new string(chars);
    }

    /// <summary>
    /// 将8字符配装码解码为ID
    /// </summary>
    /// <param name="code">8字符配装码</param>
    /// <returns>文件ID，如果编码无效返回null</returns>
    public static long? Decode(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != CodeLength)
            return null;

        long result = 0;

        foreach (var c in code)
        {
            var index = Charset.IndexOf(c);
            if (index < 0) return null; // 非法字符

            result = result * Base + index;
        }

        // 解码后加1还原ID
        return result + 1;
    }

    /// <summary>
    /// 验证配装码是否有效
    /// </summary>
    /// <param name="code">配装码</param>
    /// <returns>是否有效</returns>
    public static bool IsValid(string code)
    {
        return Decode(code).HasValue;
    }
}