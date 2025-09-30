using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

using NLog;
using NLog.Web;
using OpenQA.Selenium.Chrome;

using TactiX_Server.Data;
using TactiX_Server.Service;

namespace TactiX_Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Early init of NLog to allow startup and exception logging, before host is built
            var logger = LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
            logger.Info("Server starting...");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // 添加配置
                builder.Configuration
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddUserSecrets<Program>(optional: true);

                // 检查并处理配置
                HandleConfigure(builder);

                // Add services to the container.
                builder.Services.AddControllers();

                // 注册DbContext到服务容器
                RegisterDbContext(builder);
                // 注册HttpClient
                RegisterHttpClient(builder);

                // Add services to the container.
                builder.Services.AddHostedService<NewsGenerateService>();

                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // 启用压缩
                builder.Services.AddResponseCompression(options => 
                {
                    options.Providers.Add<GzipCompressionProvider>();
                    options.EnableForHttps = true;
                });
                builder.Services.Configure<GzipCompressionProviderOptions>(options => 
                {
                    options.Level = System.IO.Compression.CompressionLevel.Fastest;
                });

                // 添加NLog服务，自动从nlog.config文件读取配置
                builder.Logging.ClearProviders();
                builder.Host.UseNLog();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                if (!app.Environment.IsDevelopment())
                {
                    app.UseResponseCompression();
                }
                app.UseStaticFiles();
                app.UseAuthorization();
                app.MapControllers();
                app.Run();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Stopped program because of exception");
                throw;
            }
            finally
            { 
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                LogManager.Shutdown();
            }
        }

        /// <summary>
        /// 注册数据库连接
        /// </summary>
        private static void RegisterDbContext(WebApplicationBuilder builder)
        {
            var dbConnectionString = builder.Configuration["TACTIX_CONNCTION_STRINGS"];

            // 统计与状态相关
            builder.Services.AddDbContext<StatsDbContext>(options =>
            {
                options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString));
            });
            // 新闻相关
            builder.Services.AddDbContext<NewsDbContext>(options =>
            {
                options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString));
            });
        }

        /// <summary>
        /// 注册HttpClient
        /// </summary>
        private static void RegisterHttpClient(WebApplicationBuilder builder)
        {
            builder.Services.AddHttpClient("ForumClient", client =>
            {
                client.BaseAddress = new Uri("https://www.scboy.cc");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        /// <summary>
        /// 解析配置
        /// </summary>
        private static void HandleConfigure(WebApplicationBuilder builder)
        {
            builder.Services.Configure<ServerConfig>(options =>
            {
                options.ForumUserName = builder.Configuration["TACTIX_FORUM_USERNAME"]
                    ?? throw new InvalidOperationException("TACTIX_FORUM_USERNAME environment variable is required");
                options.ForumPassword = builder.Configuration["TACTIX_FORUM_PASSWORD"]
                    ?? throw new InvalidOperationException("TACTIX_FORUM_PASSWORD environment variable is required");
                options.DBConnString = builder.Configuration["TACTIX_CONNCTION_STRINGS"]
                    ?? throw new InvalidOperationException("TACTIX_CONNCTION_STRINGS environment variable is required");
            });

            builder.Services.Configure<ChromeOptions>(options => 
            {
                options.AddArgument("--headless=new");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--enable-unsafe-swiftshader");
                options.AddArgument("--disable-3d-apis");
            });
        }
    }
}
