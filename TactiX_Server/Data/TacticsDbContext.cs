using Microsoft.EntityFrameworkCore;
using TactiX_Server.Models.Tactics;

namespace TactiX_Server.Data;

/// <summary>
/// 战术大厅数据库上下文
/// </summary>
public class TacticsDbContext : DbContext
{
    public TacticsDbContext(DbContextOptions<TacticsDbContext> options) : base(options) { }

    // M1: 只需要UserLevelConfig
    public DbSet<UserLevelConfigModel> UserLevelConfigs { get; set; }

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

        // 种子数据
        SeedUserLevelConfigs(modelBuilder);
    }

    /// <summary>
    /// 种子数据：用户等级配置
    /// </summary>
    private static void SeedUserLevelConfigs(ModelBuilder modelBuilder)
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<UserLevelConfigModel>().HasData(
            new UserLevelConfigModel
            {
                Id = 1,
                LevelCode = "normal",
                LevelName = "普通用户",
                Description = "普通用户，基础权限",
                MaxFileSize = 10485760,      // 10MB
                MaxUploadCount = 10,
                MaxVersionPerFile = 5,
                DailyUploadLimit = 3,
                CanUpload = true,
                CanDeleteOwnFile = true,
                CanComment = true,
                PriorityReview = false,
                InstantNotification = false,
                BadgeColor = "#95a5a6",
                ShowInLeaderboard = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new UserLevelConfigModel
            {
                Id = 2,
                LevelCode = "verified",
                LevelName = "认证作者",
                Description = "认证作者，更高权限",
                MaxFileSize = 20971520,      // 20MB
                MaxUploadCount = 50,
                MaxVersionPerFile = 10,
                DailyUploadLimit = 10,
                CanUpload = true,
                CanDeleteOwnFile = true,
                CanComment = true,
                PriorityReview = true,
                InstantNotification = true,
                BadgeColor = "#3498db",
                ShowInLeaderboard = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new UserLevelConfigModel
            {
                Id = 3,
                LevelCode = "pro",
                LevelName = "职业选手",
                Description = "职业选手，最高权限",
                MaxFileSize = 52428800,      // 50MB
                MaxUploadCount = 200,
                MaxVersionPerFile = 20,
                DailyUploadLimit = 50,
                CanUpload = true,
                CanDeleteOwnFile = true,
                CanComment = true,
                PriorityReview = true,
                InstantNotification = true,
                BadgeColor = "#e74c3c",
                ShowInLeaderboard = true,
                CreatedAt = now,
                UpdatedAt = now
            },
            new UserLevelConfigModel
            {
                Id = 4,
                LevelCode = "admin",
                LevelName = "管理员",
                Description = "系统管理员",
                MaxFileSize = 104857600,     // 100MB
                MaxUploadCount = 1000,
                MaxVersionPerFile = 50,
                DailyUploadLimit = 100,
                CanUpload = true,
                CanDeleteOwnFile = true,
                CanComment = true,
                PriorityReview = true,
                InstantNotification = true,
                BadgeColor = "#2ecc71",
                ShowInLeaderboard = true,
                CreatedAt = now,
                UpdatedAt = now
            }
        );
    }
}