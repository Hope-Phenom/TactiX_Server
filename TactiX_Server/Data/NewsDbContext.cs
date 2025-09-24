using Microsoft.EntityFrameworkCore;
using TactiX_Server.Models.News;

namespace TactiX_Server.Data
{
    public class NewsDbContext : DbContext
    {
        public NewsDbContext(DbContextOptions<NewsDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NewsCommunityModel>()
                .ToTable("news_community");
            modelBuilder.Entity<NewsCommunityModel>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd(); // 配置为添加时自动生成值（自增）

            modelBuilder.Entity<ConfigVideoUpModel>()
                .ToTable("config_video_up");
            modelBuilder.Entity<ConfigVideoUpModel>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd(); // 配置为添加时自动生成值（自增）
        }

        public DbSet<NewsCommunityModel> NewsCommunityItems { get; set; }
        public DbSet<ConfigVideoUpModel> ConfigVideoUpModels { get; set; }
    }
}
