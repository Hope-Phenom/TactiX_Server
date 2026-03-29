using Microsoft.EntityFrameworkCore;

using TactiX_Server.Data;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// 权限服务接口
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// 获取用户等级配置
    /// </summary>
    Task<UserLevelConfigModel?> GetUserLevelConfig(long userId);

    /// <summary>
    /// 检查用户是否有上传权限
    /// </summary>
    Task<PermissionCheckResult> CanUpload(long userId, long fileSize);

    /// <summary>
    /// 检查用户是否可以上传新版本
    /// </summary>
    Task<PermissionCheckResult> CanUploadVersion(long userId, long fileId);

    /// <summary>
    /// 检查用户是否可以删除自己的文件
    /// </summary>
    Task<PermissionCheckResult> CanDeleteOwnFile(long userId, long fileId);

    /// <summary>
    /// 获取用户今日上传数量
    /// </summary>
    Task<int> GetTodayUploadCount(long userId);

    /// <summary>
    /// 获取用户总上传数量
    /// </summary>
    Task<int> GetTotalUploadCount(long userId);
}

/// <summary>
/// 权限服务实现
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly TacticsDbContext _context;

    public PermissionService(TacticsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<UserLevelConfigModel?> GetUserLevelConfig(long userId)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return await _context.UserLevelConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LevelCode == user.LevelCode);
    }

    /// <inheritdoc />
    public async Task<PermissionCheckResult> CanUpload(long userId, long fileSize)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return PermissionCheckResult.Fail("用户不存在");

        if (user.IsActive != 1)
            return PermissionCheckResult.Fail("用户账号已被禁用");

        var levelConfig = await _context.UserLevelConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LevelCode == user.LevelCode);

        if (levelConfig == null)
            return PermissionCheckResult.Fail("用户等级配置不存在");

        if (levelConfig.CanUpload != 1)
            return PermissionCheckResult.Fail("您没有上传权限");

        // 检查单文件大小限制
        if (fileSize > levelConfig.MaxFileSize)
            return PermissionCheckResult.Fail($"文件大小超过限制，最大允许 {FormatFileSize(levelConfig.MaxFileSize)}");

        // 检查总上传数量
        var totalUploads = await GetTotalUploadCount(userId);
        if (totalUploads >= levelConfig.MaxUploadCount)
            return PermissionCheckResult.Fail($"已达到上传数量上限 ({levelConfig.MaxUploadCount}个)，请联系管理员升级等级");

        // 检查今日上传数量
        var todayUploads = await GetTodayUploadCount(userId);
        if (todayUploads >= levelConfig.DailyUploadLimit)
            return PermissionCheckResult.Fail($"今日上传次数已用完，每日限制 {levelConfig.DailyUploadLimit} 次");

        return PermissionCheckResult.Success();
    }

    /// <inheritdoc />
    public async Task<PermissionCheckResult> CanUploadVersion(long userId, long fileId)
    {
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (file == null)
            return PermissionCheckResult.Fail("战术文件不存在");

        if (file.UploaderId != userId)
            return PermissionCheckResult.Fail("只能为自己上传的文件添加新版本");

        var levelConfig = await GetUserLevelConfig(userId);
        if (levelConfig == null)
            return PermissionCheckResult.Fail("用户等级配置不存在");

        // 获取当前版本数量
        var versionCount = await _context.TacticsFileVersions
            .AsNoTracking()
            .CountAsync(v => v.FileId == fileId);

        if (versionCount >= levelConfig.MaxVersionPerFile)
            return PermissionCheckResult.Fail($"已达到版本数量上限 ({levelConfig.MaxVersionPerFile}个)");

        return PermissionCheckResult.Success();
    }

    /// <inheritdoc />
    public async Task<PermissionCheckResult> CanDeleteOwnFile(long userId, long fileId)
    {
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (file == null)
            return PermissionCheckResult.Fail("战术文件不存在");

        if (file.UploaderId != userId)
            return PermissionCheckResult.Fail("只能删除自己上传的文件");

        var levelConfig = await GetUserLevelConfig(userId);
        if (levelConfig == null)
            return PermissionCheckResult.Fail("用户等级配置不存在");

        if (levelConfig.CanDeleteOwnFile != 1)
            return PermissionCheckResult.Fail("您没有删除文件的权限");

        return PermissionCheckResult.Success();
    }

    /// <inheritdoc />
    public async Task<int> GetTodayUploadCount(long userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.TacticsFiles
            .AsNoTracking()
            .CountAsync(f => f.UploaderId == userId && f.CreatedAt >= today);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalUploadCount(long userId)
    {
        return await _context.TacticsFiles
            .AsNoTracking()
            .CountAsync(f => f.UploaderId == userId);
    }

    /// <summary>
    /// 格式化文件大小
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 权限检查结果
/// </summary>
public class PermissionCheckResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public static PermissionCheckResult Success() => new() { Success = true };
    public static PermissionCheckResult Fail(string message) => new() { Success = false, ErrorMessage = message };
}
