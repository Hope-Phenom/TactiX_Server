using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// 管理员服务接口
/// </summary>
public interface IAdminService
{
    /// <summary>检查用户是否为超级管理员（通过名称）</summary>
    bool IsSuperAdminByName(string? nickname);

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

    public async Task<bool> IsAdminAsync(long userId)
    {
        var admin = await _context.TacticsAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return admin != null;
    }

    public async Task<bool> IsSuperAdminAsync(long userId)
    {
        var admin = await _context.TacticsAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return admin?.Role == "super_admin";
    }

    public async Task<string?> GetAdminRoleAsync(long userId)
    {
        var admin = await _context.TacticsAdmins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId);

        return admin?.Role;
    }
}