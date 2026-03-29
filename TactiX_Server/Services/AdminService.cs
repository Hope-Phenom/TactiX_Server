using Microsoft.EntityFrameworkCore;
using TactiX_Server.Data;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Services;

/// <summary>
/// 管理员权限服务
/// </summary>
public interface IAdminService
{
    /// <summary>检查用户是否为管理员</summary>
    Task<bool> IsAdminAsync(long userId);

    /// <summary>检查用户是否为超级管理员</summary>
    Task<bool> IsSuperAdminAsync(long userId);

    /// <summary>根据用户名检查是否为超级管理员</summary>
    bool IsSuperAdminByName(string? nickname);

    /// <summary>获取管理员角色</summary>
    Task<string?> GetAdminRoleAsync(long userId);
}

/// <summary>
/// 管理员权限服务实现
/// </summary>
public class AdminService : IAdminService
{
    private readonly TacticsDbContext _context;
    private readonly ILogger<AdminService> _logger;

    public AdminService(TacticsDbContext context, ILogger<AdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> IsAdminAsync(long userId)
    {
        return await _context.TacticsAdmins.AnyAsync(a => a.UserId == userId);
    }

    public async Task<bool> IsSuperAdminAsync(long userId)
    {
        var admin = await _context.TacticsAdmins
            .FirstOrDefaultAsync(a => a.UserId == userId);
        return admin?.Role == "super_admin";
    }

    public bool IsSuperAdminByName(string? nickname)
    {
        // 硬编码的超级管理员标识
        return !string.IsNullOrEmpty(nickname) &&
               nickname.Equals(TacticsAdminModel.SuperAdminIdentifier, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string?> GetAdminRoleAsync(long userId)
    {
        var admin = await _context.TacticsAdmins
            .FirstOrDefaultAsync(a => a.UserId == userId);
        return admin?.Role;
    }
}
