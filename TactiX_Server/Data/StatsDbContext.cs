using Microsoft.EntityFrameworkCore;
using TactiX_Server.Models.Stats;

namespace TactiX_Server.Data
{
    public class StatsDbContext : DbContext
    {
        public StatsDbContext(DbContextOptions<StatsDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 异常上报
            modelBuilder.Entity<ExceptionReportModel>()
                .ToTable("stats_excption_report");
            modelBuilder.Entity<ExceptionReportModel>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd(); // 配置为添加时自动生成值（自增）

            // 版本控制
            modelBuilder.Entity<VersionControlModel>()
                .ToTable("stats_version_control");
            modelBuilder.Entity<VersionControlModel>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd(); // 配置为添加时自动生成值（自增）
        }

        public DbSet<ExceptionReportModel> ExceptionReports { get; set; }
        public DbSet<VersionControlModel> VersionControls { get; set; }
    }
}
