using Microsoft.EntityFrameworkCore;
using TactiX_Server.Models.Tactics;
using TactiX_Server.Services;

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

    // 互动相关
    public DbSet<TacticsLikeModel> TacticsLikes { get; set; }
    public DbSet<TacticsFavoriteModel> TacticsFavorites { get; set; }
    public DbSet<TacticsCommentModel> TacticsComments { get; set; }

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

            entity.HasOne(e => e.LevelConfig)
                .WithMany()
                .HasForeignKey(e => e.LevelCode)
                .HasPrincipalKey(e => e.LevelCode);
        });

        // 管理员
        modelBuilder.Entity<TacticsAdminModel>(entity =>
        {
            entity.ToTable("tactics_admin");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Role);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 战术文件
        modelBuilder.Entity<TacticsFileModel>(entity =>
        {
            entity.ToTable("tactics_file");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ShareCode).IsUnique();
            entity.HasIndex(e => e.UploaderId);
            entity.HasIndex(e => e.Race);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.FileHash);

            entity.Property(e => e.ShareCode).HasMaxLength(8).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Author).HasMaxLength(128);
            entity.Property(e => e.Race).HasMaxLength(1);
            entity.Property(e => e.FilePath).HasMaxLength(512).IsRequired();
            entity.Property(e => e.FileHash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(16).IsRequired();

            entity.HasOne(e => e.Uploader)
                .WithMany()
                .HasForeignKey(e => e.UploaderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.LatestVersion)
                .WithMany()
                .HasForeignKey(e => e.LatestVersionId);
        });

        // 战术文件版本
        modelBuilder.Entity<TacticsFileVersionModel>(entity =>
        {
            entity.ToTable("tactics_file_version");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FileId, e.VersionNumber }).IsUnique();
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.FileHash);

            entity.Property(e => e.FilePath).HasMaxLength(512).IsRequired();
            entity.Property(e => e.FileHash).HasMaxLength(64).IsRequired();

            entity.HasOne(e => e.File)
                .WithMany(e => e.Versions)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 点赞记录
        modelBuilder.Entity<TacticsLikeModel>(entity =>
        {
            entity.ToTable("tactics_like");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.FileId }).IsUnique();
            entity.HasIndex(e => e.FileId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.File)
                .WithMany()
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 收藏记录
        modelBuilder.Entity<TacticsFavoriteModel>(entity =>
        {
            entity.ToTable("tactics_favorite");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.FileId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.FileId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.File)
                .WithMany()
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // 评论记录
        modelBuilder.Entity<TacticsCommentModel>(entity =>
        {
            entity.ToTable("tactics_comment");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FileId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ParentCommentId);

            entity.Property(e => e.Content).HasMaxLength(1000).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.File)
                .WithMany()
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ParentComment)
                .WithMany(e => e.Replies)
                .HasForeignKey(e => e.ParentCommentId)
                .OnDelete(DeleteBehavior.SetNull);
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
                LevelCode = UserLevels.Normal,
                LevelName = "普通用户",
                Description = "普通用户，基础权限",
                MaxFileSize = 10485760,
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
                LevelCode = UserLevels.Verified,
                LevelName = "认证作者",
                Description = "认证作者，更高权限",
                MaxFileSize = 20971520,
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
                LevelCode = UserLevels.Pro,
                LevelName = "职业选手",
                Description = "职业选手，最高权限",
                MaxFileSize = 52428800,
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
                LevelCode = UserLevels.Admin,
                LevelName = "管理员",
                Description = "系统管理员",
                MaxFileSize = 104857600,
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