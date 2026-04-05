using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// 权限检查结果
/// </summary>
public class PermissionCheckResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static PermissionCheckResult SucceededResult() => new() { Succeeded = true };
    public static PermissionCheckResult Failed(string message) => new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>
/// 权限服务接口
/// </summary>
public interface IPermissionService
{
    /// <summary>获取用户等级配置</summary>
    Task<UserLevelConfigModel?> GetUserLevelConfigAsync(long userId);

    /// <summary>检查用户是否有上传权限</summary>
    Task<PermissionCheckResult> CanUploadAsync(long userId, long fileSize);

    /// <summary>检查用户是否可以上传新版本（上传者或管理员）</summary>
    Task<PermissionCheckResult> CanUploadVersionAsync(long userId, long fileId);

    /// <summary>检查用户是否可以删除文件（上传者或管理员）</summary>
    Task<PermissionCheckResult> CanDeleteFileAsync(long userId, long fileId);

    /// <summary>获取用户今日上传数量</summary>
    Task<int> GetTodayUploadCountAsync(long userId);

    /// <summary>获取用户总上传数量</summary>
    Task<int> GetTotalUploadCountAsync(long userId);
}

/// <summary>
/// 权限服务实现
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly TacticsDbContext _context;
    private readonly IAdminService _adminService;

    public PermissionService(TacticsDbContext context, IAdminService adminService)
    {
        _context = context;
        _adminService = adminService;
    }

    public async Task<UserLevelConfigModel?> GetUserLevelConfigAsync(long userId)
    {
        var user = await _context.TacticsUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return null;

        return await _context.UserLevelConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LevelCode == user.LevelCode);
    }

    public async Task<PermissionCheckResult> CanUploadAsync(long userId, long fileSize)
    {
        var user = await _context.TacticsUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return PermissionCheckResult.Failed("用户不存在");

        if (!user.IsActive)
            return PermissionCheckResult.Failed("用户账号已被禁用");

        var levelConfig = await _context.UserLevelConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.LevelCode == user.LevelCode);

        if (levelConfig == null)
            return PermissionCheckResult.Failed("用户等级配置不存在");

        if (!levelConfig.CanUpload)
            return PermissionCheckResult.Failed("您没有上传权限");

        // 检查单文件大小限制
        if (fileSize > levelConfig.MaxFileSize)
            return PermissionCheckResult.Failed($"文件大小超过限制，最大允许 {FormatFileSize(levelConfig.MaxFileSize)}");

        // 检查总上传数量
        var totalUploads = await GetTotalUploadCountAsync(userId);
        if (totalUploads >= levelConfig.MaxUploadCount)
            return PermissionCheckResult.Failed($"已达到上传数量上限 ({levelConfig.MaxUploadCount}个)，请联系管理员升级等级");

        // 检查今日上传数量
        var todayUploads = await GetTodayUploadCountAsync(userId);
        if (todayUploads >= levelConfig.DailyUploadLimit)
            return PermissionCheckResult.Failed($"今日上传次数已用完，每日限制 {levelConfig.DailyUploadLimit} 次");

        return PermissionCheckResult.SucceededResult();
    }

    public async Task<PermissionCheckResult> CanUploadVersionAsync(long userId, long fileId)
    {
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (file == null)
            return PermissionCheckResult.Failed("战术文件不存在");

        // 管理员可以为任何文件上传新版本
        var isAdmin = await _adminService.IsAdminAsync(userId);
        if (isAdmin)
        {
            var adminLevelConfig = await GetUserLevelConfigAsync(userId);
            if (adminLevelConfig == null)
                return PermissionCheckResult.Failed("用户等级配置不存在");

            var versionCount = await _context.TacticsFileVersions
                .AsNoTracking()
                .CountAsync(v => v.FileId == fileId);

            if (versionCount >= adminLevelConfig.MaxVersionPerFile)
                return PermissionCheckResult.Failed($"已达到版本数量上限 ({adminLevelConfig.MaxVersionPerFile}个)");

            return PermissionCheckResult.SucceededResult();
        }

        // 非管理员只能为自己的文件上传版本
        if (file.UploaderId != userId)
            return PermissionCheckResult.Failed("只能为自己上传的文件添加新版本");

        var levelConfig = await GetUserLevelConfigAsync(userId);
        if (levelConfig == null)
            return PermissionCheckResult.Failed("用户等级配置不存在");

        // 获取当前版本数量
        var currentVersionCount = await _context.TacticsFileVersions
            .AsNoTracking()
            .CountAsync(v => v.FileId == fileId);

        if (currentVersionCount >= levelConfig.MaxVersionPerFile)
            return PermissionCheckResult.Failed($"已达到版本数量上限 ({levelConfig.MaxVersionPerFile}个)");

        return PermissionCheckResult.SucceededResult();
    }

    public async Task<PermissionCheckResult> CanDeleteFileAsync(long userId, long fileId)
    {
        var file = await _context.TacticsFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fileId);

        if (file == null)
            return PermissionCheckResult.Failed("战术文件不存在");

        // 管理员可以删除任何文件
        var isAdmin = await _adminService.IsAdminAsync(userId);
        if (isAdmin)
            return PermissionCheckResult.SucceededResult();

        // 非管理员只能删除自己的文件
        if (file.UploaderId != userId)
            return PermissionCheckResult.Failed("只能删除自己上传的文件");

        var levelConfig = await GetUserLevelConfigAsync(userId);
        if (levelConfig == null)
            return PermissionCheckResult.Failed("用户等级配置不存在");

        if (!levelConfig.CanDeleteOwnFile)
            return PermissionCheckResult.Failed("您没有删除文件的权限");

        return PermissionCheckResult.SucceededResult();
    }

    public async Task<int> GetTodayUploadCountAsync(long userId)
    {
        var today = DateTime.UtcNow.Date;
        return await _context.TacticsFiles
            .AsNoTracking()
            .CountAsync(f => f.UploaderId == userId && f.CreatedAt >= today);
    }

    public async Task<int> GetTotalUploadCountAsync(long userId)
    {
        return await _context.TacticsFiles
            .AsNoTracking()
            .CountAsync(f => f.UploaderId == userId && !f.IsDeleted);
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