using Microsoft.EntityFrameworkCore;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Data;

/// <summary>
/// 战术大厅数据库上下文
/// </summary>
public class TacticsDbContext : DbContext
{
    public TacticsDbContext(DbContextOptions<TacticsDbContext> options) : base(options) { }

    // 用户相关
    public DbSet<UserLevelConfigModel> UserLevelConfigs { get; set; }
    public DbSet<TacticsUserModel> TacticsUsers { get; set; }
    public DbSet<TacticsAdminModel> TacticsAdmins { get; set; }

    // 文件相关
    public DbSet<TacticsFileModel> TacticsFiles { get; set; }
    public DbSet<TacticsFileVersionModel> TacticsFileVersions { get; set; }
    public DbSet<TacticsLikeModel> TacticsLikes { get; set; }
    public DbSet<TacticsAuditLogModel> TacticsAuditLogs { get; set; }

    // 通知相关
    public DbSet<NotificationConfigModel> NotificationConfigs { get; set; }
    public DbSet<NotificationLogModel> NotificationLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 用户等级配置
        modelBuilder.Entity<UserLevelConfigModel>(entity =>
        {
            entity.ToTable("user_level_config");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LevelCode).HasMaxLength(32).IsRequired();
            entity.HasIndex(e => e.LevelCode).IsUnique();
        });

        // 用户
        modelBuilder.Entity<TacticsUserModel>(entity =>
        {
            entity.ToTable("tactics_user");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.OAuthProvider, e.OAuthId }).IsUnique();
            entity.HasIndex(e => e.LevelCode);
        });

        // 管理员
        modelBuilder.Entity<TacticsAdminModel>(entity =>
        {
            entity.ToTable("tactics_admin");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Role);
        });

        // 战术文件
        modelBuilder.Entity<TacticsFileModel>(entity =>
        {
            entity.ToTable("tactics_file");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShareCode).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.RacePlayed);
            entity.HasIndex(e => e.RaceOpponent);
            entity.HasIndex(e => e.Matchup);
            entity.HasIndex(e => e.TacticType);
            entity.HasIndex(e => e.UploaderId);
        });

        // 文件版本
        modelBuilder.Entity<TacticsFileVersionModel>(entity =>
        {
            entity.ToTable("tactics_file_version");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FileId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.FileId);
        });

        // 点赞
        modelBuilder.Entity<TacticsLikeModel>(entity =>
        {
            entity.ToTable("tactics_like");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.FileId }).IsUnique();
            entity.HasIndex(e => e.FileId);
        });

        // 审核日志
        modelBuilder.Entity<TacticsAuditLogModel>(entity =>
        {
            entity.ToTable("tactics_audit_log");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileId);
        });

        // 通知配置
        modelBuilder.Entity<NotificationConfigModel>(entity =>
        {
            entity.ToTable("notification_config");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ConfigKey).IsUnique();
        });

        // 通知日志
        modelBuilder.Entity<NotificationLogModel>(entity =>
        {
            entity.ToTable("notification_log");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.NotificationType, e.EventType });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}
