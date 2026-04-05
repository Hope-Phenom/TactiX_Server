using System.Security.Cryptography;
using System.Text;

namespace TactiX_Server.Utils;

/// <summary>
/// 哈希工具类
/// 提供文件和字符串的SHA256哈希计算功能
/// </summary>
public static class HashUtil
{
    /// <summary>
    /// 计算字节数组的SHA256哈希值
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <returns>小写十六进制字符串（64字符）</returns>
    public static string ComputeSha256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// 计算字符串的SHA256哈希值
    /// </summary>
    /// <param name="content">原始字符串（UTF-8编码）</param>
    /// <returns>小写十六进制字符串（64字符）</returns>
    public static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return ComputeSha256(bytes);
    }

    /// <summary>
    /// 计算流的SHA256哈希值
    /// </summary>
    /// <param name="stream">数据流</param>
    /// <returns>小写十六进制字符串（64字符）</returns>
    public static async Task<string> ComputeSha256Async(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// 计算文件的SHA256哈希值
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>小写十六进制字符串（64字符）</returns>
    public static async Task<string> ComputeFileSha256Async(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        return await ComputeSha256Async(stream);
    }
}