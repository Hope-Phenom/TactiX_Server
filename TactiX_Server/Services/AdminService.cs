using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// 用户等级常量
/// </summary>
public static class UserLevels
{
    public const string Normal = "normal";
    public const string Verified = "verified";
    public const string Pro = "pro";
    public const string Admin = "admin";
}

/// <summary>
/// 管理员角色常量
/// </summary>
public static class AdminRoles
{
    public const string SuperAdmin = "super_admin";
    public const string Admin = "admin";
    public const string Moderator = "moderator";
}

/// <summary>
/// 管理员服务接口
/// </summary>
public interface IAdminService
{
    /// <summary>检查用户是否为超级管理员（通过名称）</summary>
    bool IsSuperAdminByName(string? nickname);

    /// <summary>获取用户管理员信息（单次查询）</summary>
    Task<TacticsAdminModel?> GetAdminAsync(long userId);

    /// <summary>检查用户是否为管理员</summary>
    Task<bool> IsAdminAsync(long userId);

    /// <summary>检查用户是否为超级管理员</summary>
    Task<bool> IsSuperAdminAsync(long userId);

    /// <summary>获取用户管理员角色</summary>
    Task<string?> GetAdminRoleAsync(long userId);
}

/// <summary>
/// 管理员服务实现
/// </summary>
public class AdminService : IAdminService
{
    private readonly TacticsDbContext _context;

    // 超级管理员硬编码列表
    private static readonly HashSet<string> SuperAdminNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "hope_phenom"
    };

    public AdminService(TacticsDbContext context)
    {
        _context = context;
    }

    public bool IsSuperAdminByName(string? nickname)
    {
        if (string.IsNullOrEmpty(nickname)) return false;
        return SuperAdminNames.Contains(nickname);
    }

    public async Task<TacticsAdminModel?> GetAdminAsync(long userId)
    {
        return await _context.TacticsAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    public async Task<bool> IsAdminAsync(long userId)
    {
        return await GetAdminAsync(userId) != null;
    }

    public async Task<bool> IsSuperAdminAsync(long userId)
    {
        var admin = await GetAdminAsync(userId);
        return admin?.Role == AdminRoles.SuperAdmin;
    }

    public async Task<string?> GetAdminRoleAsync(long userId)
    {
        var admin = await GetAdminAsync(userId);
        return admin?.Role;
    }
}