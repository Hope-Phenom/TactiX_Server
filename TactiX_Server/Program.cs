using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;

using NLog;
using NLog.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

using TactiX_Server.Data;
using TactiX_Server.Middleware;
using TactiX_Server.Models.Config;
using TactiX_Server.Service;
using TactiX_Server.Services;

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

                // Configuration
                builder.Configuration
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                    .AddEnvironmentVariables()
                    .AddUserSecrets<Program>(optional: true);

                // Handle configure
                HandleConfigure(builder);

                // Add services to the container.
                builder.Services.AddControllers();

                // Register DbContext
                RegisterDbContext(builder);
                // Register HttpClient
                RegisterHttpClient(builder);
                // Register Tactics Hall Services (M2)
                RegisterTacticsHallServices(builder);

                // Add services to the container.
                builder.Services.AddHostedService<NewsGenerateService>();

                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // Response compression
                builder.Services.AddResponseCompression(options =>
                {
                    options.Providers.Add<GzipCompressionProvider>();
                    options.EnableForHttps = true;
                });
                builder.Services.Configure<GzipCompressionProviderOptions>(options =>
                {
                    options.Level = System.IO.Compression.CompressionLevel.Fastest;
                });

                // Configure NLog
                builder.Logging.ClearProviders();
                builder.Host.UseNLog();

                var app = builder.Build();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // Global exception handler
                app.UseExceptionHandler("/error");
                app.Map("/error", (HttpContext context) =>
                {
                    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
                    return Results.Problem(
                        title: "An error occurred",
                        detail: exception?.Message,
                        statusCode: 500);
                });

                app.UseHttpsRedirection();
                if (!app.Environment.IsDevelopment())
                {
                    app.UseResponseCompression();
                }
                app.UseStaticFiles();

                // Add authentication and authorization
                app.UseAuthentication();
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
        /// Register DbContext
        /// </summary>
        private static void RegisterDbContext(WebApplicationBuilder builder)
        {
            var dbConnectionString = builder.Configuration["TACTIX_CONNCTION_STRINGS"];

            // Stats DbContext
            builder.Services.AddDbContextPool<StatsDbContext>(options =>
            {
                options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString),
                    mysqlOptions =>
                    {
                        mysqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                        mysqlOptions.CommandTimeout(60);
                    });
            }, poolSize: 128);

            // News DbContext
            builder.Services.AddDbContextPool<NewsDbContext>(options =>
            {
                options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString),
                    mysqlOptions =>
                    {
                        mysqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                        mysqlOptions.CommandTimeout(60);
                    });
            }, poolSize: 128);

            // Tactics Hall DbContext
            builder.Services.AddDbContextPool<TacticsDbContext>(options =>
            {
                options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString),
                    mysqlOptions =>
                    {
                        mysqlOptions.EnableRetryOnFailure(maxRetryCount: 3);
                        mysqlOptions.CommandTimeout(60);
                    });
            }, poolSize: 128);
        }

        /// <summary>
        /// Register HttpClient
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
        /// Handle Configure
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
                options.AddArgument("--no-sandbox");
                options.AddArgument("--disable-dev-shm-usage");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--disable-extensions");
                options.AddArgument("--disable-software-rasterizer");
                options.AddArgument("--disable-features=VizDisplayCompositor");
                options.AddArgument("--disable-setuid-sandbox");
                options.AddArgument("--single-process");
                options.AddArgument("--disable-blink-features=AutomationControlled");
                options.AddArgument("--disable-web-security");
                options.AddArgument("--ignore-certificate-errors");
                options.AddArgument("--allow-running-insecure-content");
                options.AddArgument("--disable-notifications");
                options.AddArgument("--disable-popup-blocking");
                options.AddArgument("--disable-infobars");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                options.AddExcludedArgument("enable-automation");
                options.AddAdditionalOption("useAutomationExtension", false);

                options.PageLoadStrategy = PageLoadStrategy.Normal;
                options.UnhandledPromptBehavior = UnhandledPromptBehavior.Accept;
            });
        }

        /// <summary>
        /// Register Tactics Hall Services (M2)
        /// </summary>
        private static void RegisterTacticsHallServices(WebApplicationBuilder builder)
        {
            // JWT Authentication
            builder.Services.AddJwtAuthentication(builder.Configuration);

            // Services
            builder.Services.AddScoped<IJwtService, JwtService>();
            builder.Services.AddScoped<IAdminService, AdminService>();

            // OAuth Providers
            builder.Services.AddScoped<IOAuthProvider, DevAuthService>();
        }
    }
}