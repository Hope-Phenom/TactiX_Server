using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices;
using TactiX_Server.Data;
using TactiX_Server.Models.News;

namespace TactiX_Server.Service
{
    public class NewsGenerateService : BackgroundService
    {
        private readonly ILogger<NewsGenerateService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ServerConfig _options;
        private readonly ChromeOptions _chromeOptions; 
        private readonly IServiceScopeFactory _scopeFactory;

        private bool _firstTime = true;

        public NewsGenerateService(ILogger<NewsGenerateService> logger,
            IHttpClientFactory httpClientFactory,
            IOptions<ServerConfig> options,
            IOptions<ChromeOptions> chromeOptions,
            IServiceScopeFactory scopeFactory)
        { 
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _chromeOptions = chromeOptions.Value;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_firstTime)
                {
                    _logger.LogInformation("服务端启动，资讯生成延迟执行");
                    Thread.Sleep(30000);
                    _firstTime = false;
                }

                _logger.LogInformation("资讯生成定时任务执行中: {time}", DateTime.Now);

                await UpdateCommunityScboyccNews();
                await UpdateBiliBiliVideo();

                // 每8小时执行一次
                await Task.Delay(TimeSpan.FromHours(8), stoppingToken);
            }
        }

        /// <summary>
        /// 获取scboy论坛的热帖
        /// </summary>
        private async Task UpdateCommunityScboyccNews()
        {
            await Task.Run(async () =>
            {
                try
                {
                    // 1. 创建配置了Cookie的HttpClient
                    var httpClient = _httpClientFactory.CreateClient("ForumClient");

                    // 创建独立的Cookie容器
                    var cookieContainer = new CookieContainer();
                    var handler = new HttpClientHandler
                    {
                        UseCookies = true,
                        CookieContainer = cookieContainer
                    };

                    // 创建使用自定义Handler的HttpClient
                    using var customHttpClient = new HttpClient(handler);
                    customHttpClient.BaseAddress = httpClient.BaseAddress;
                    customHttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(httpClient.DefaultRequestHeaders.UserAgent.ToString());
                    customHttpClient.Timeout = httpClient.Timeout;

                    // 2. 获取登录页面并解析CSRF token
                    _logger.LogInformation("Fetching login page...");
                    var loginPageResponse = await customHttpClient.GetAsync("/?user-login.htm");
                    loginPageResponse.EnsureSuccessStatusCode();

                    var loginPageHtml = await loginPageResponse.Content.ReadAsStringAsync();
                    var parser = new HtmlParser();
                    var loginDocument = await parser.ParseDocumentAsync(loginPageHtml);

                    // 查找CSRF token
                    var csrfToken = loginDocument
                        .QuerySelector("input[name='csrf_token']")?
                        .GetAttribute("value");

                    if (string.IsNullOrEmpty(csrfToken))
                    {
                        _logger.LogWarning("CSRF token not found. Trying alternative selectors...");
                        // 尝试其他常见名称
                        csrfToken = loginDocument
                            .QuerySelector("input[name='authenticity_token'], input[name='_token'], input[name='csrfmiddlewaretoken']")?
                            .GetAttribute("value");
                    }

                    var usr = _options.ForumUserName;
                    var pwd = _options.ForumPassword;

                    // 3. 准备登录表单数据
                    var formData = new Dictionary<string, string>
                    {
                        ["mobile"] = usr,
                        ["password"] = pwd
                    };

                    // 添加CSRF token（如果找到）
                    if (!string.IsNullOrEmpty(csrfToken))
                    {
                        formData["csrf_token"] = csrfToken;
                    }

                    // 4. 执行登录
                    _logger.LogInformation("Attempting login...");
                    var loginContent = new FormUrlEncodedContent(formData);
                    var loginResponse = await customHttpClient.PostAsync("/?user-login.htm", loginContent);

                    if (!loginResponse.IsSuccessStatusCode)
                    {
                        _logger.LogError($"Login failed with status: {loginResponse.StatusCode}");
                    }

                    // 5. 访问需要登录的帖子列表页面
                    _logger.LogInformation("Fetching posts page...");
                    var postsResponse = await customHttpClient.GetAsync("/?forum-1.htm");
                    postsResponse.EnsureSuccessStatusCode();

                    var postsHtml = await postsResponse.Content.ReadAsStringAsync();
                    var postsDocument = await parser.ParseDocumentAsync(postsHtml);

                    // 6. 解析帖子列表
                    var posts = new List<ForumPost>();
                    var allPostElements = postsDocument.QuerySelectorAll("li.media.thread");

                    // 7.使用 LINQ 过滤掉包含 top_x 类名的元素
                    var postElements = allPostElements.Where(element =>
                    {
                        var classList = element.ClassList;
                        return !classList.Contains("top_1") &&
                               !classList.Contains("top_2") &&
                               !classList.Contains("top_3");
                    });

                    foreach (var element in postElements)
                    {
                        try
                        {
                            // 提取标题
                            var titleElement = element.QuerySelector("div.subject > a.xs-thread-a");
                            string title = titleElement?.TextContent.Trim() ?? string.Empty;

                            // 提取链接
                            string url = titleElement?.GetAttribute("href") ?? string.Empty;

                            // 提取时间 - 处理多种情况
                            string date = "";
                            var dateContainer = element.QuerySelector("div.d-flex.justify-content-between.small.mt-1 div:first-child");

                            if (dateContainer != null)
                            {
                                // 尝试获取直接显示的时间
                                var visibleDate = dateContainer.QuerySelector(".date.text-grey:not(.hidden-sm)");
                                if (visibleDate != null)
                                {
                                    date = visibleDate.TextContent.Trim();
                                }
                                else
                                {
                                    // 尝试获取隐藏的时间（在hidden-sm类中）
                                    var hiddenDate = dateContainer.QuerySelector(".date.text-grey.hidden-sm");
                                    if (hiddenDate != null)
                                    {
                                        date = hiddenDate.TextContent.Trim();
                                    }
                                }
                            }

                            // 创建帖子对象
                            var post = new ForumPost
                            {
                                Title = title,
                                Url = httpClient.BaseAddress + url,
                                Date = date
                            };

                            posts.Add(post);
                        }
                        catch (Exception ex)
                        {
                            // 记录错误但继续处理其他帖子
                            _logger.LogError($"解析帖子时出错: {ex.Message}");
                        }
                    }

                    var record = new NewsCommunityModel()
                    {
                        Update_DateTime = DateTime.Now,
                        Type = 0,
                        Json = JsonConvert.SerializeObject(posts.Take(20).ToList())
                    };

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        // 添加record
                        var context = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
                        context.NewsCommunityItems.Add(record);
                        await context.SaveChangesAsync();
                    }

                    _logger.LogInformation("Update forum posts success.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching forum posts");
                }
            });
        }
        /// <summary>
        /// 获取数据库配置的Bilibili视频主的更新情况
        /// </summary>
        /// <returns></returns>
        private async Task UpdateBiliBiliVideo()
        {
            await Task.Run(async () =>
            {
                KillAllChromeProcesses();

                using var scope = _scopeFactory.CreateScope();
                using var driver = new ChromeDriver(_chromeOptions);
                var list = new List<VideoInfo>();

                // 等待Chrome启动完毕
                Thread.Sleep(10000);

                try
                {
                    var context = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
                    var ups = await context.ConfigVideoUpModels
                        .Where(e => e.Type == 0)
                        .ToListAsync();

                    foreach (var up in ups)
                    {
                        list.AddRange(await ResolveBiliBiliSpace(driver, up));
                        Thread.Sleep(1000);
                    }

                    // 获取当前时间快照（用于统一时间基准）
                    DateTime now = DateTime.Now;

                    // 解析并排序
                    var sortedVideos = list
                        .Select(v => new
                        {
                            Video = v,
                            SortKey = ParsePublishDate(v.PublishDate, now) // 解析为排序键
                        })
                        .OrderBy(x => x.SortKey.Category)  // 先按类别排序
                        .ThenBy(x => x.SortKey.Value)      // 再按类别内规则排序
                        .Select(x => x.Video)              // 还原为VideoInfo
                        .Take(6)                           // 取前6项
                        .ToList();

                    driver.Quit();

                    var record = new NewsCommunityModel()
                    {
                         Type = 1,
                         Update_DateTime = now,
                         Json = JsonConvert.SerializeObject(sortedVideos)
                    };

                    context.NewsCommunityItems.Add(record);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Update bilibili video success.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching bilibili video info");
                }
            });
        }

        /// <summary>
        /// 解析指定的BilibiliSpace地址
        /// </summary>
        /// <param name="driver">ChromeDriver</param>
        /// <param name="url">BilibiliSpace地址</param>
        /// <returns>视频信息列表</returns>
        private async Task<List<VideoInfo>> ResolveBiliBiliSpace(ChromeDriver driver, ConfigVideoUpModel up)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var list = new List<VideoInfo>();

                    // 1. 导航到目标网页
                    driver.Navigate().GoToUrl(up.Url);
                    driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(7); // 隐式等待

                    // 2. 定位元素并提取数据
                    var elements = driver.FindElements(By.CssSelector(".items__item"));

                    foreach (var element in elements)
                    {
                        try
                        {
                            // 封面图
                            var img = element.FindElement(By.TagName("img"));
                            if (img == null) continue;
                            var imgSrc = img.GetAttribute("src") ?? string.Empty;
                            var coverUrl = imgSrc.Substring(0, imgSrc.IndexOf("@"));

                            // 发布日期
                            var subtitle = element.FindElement(By.ClassName("bili-video-card__subtitle"));
                            if (subtitle == null) continue;
                            var dateSpan = subtitle.FindElement(By.TagName("span"));
                            var dateText = dateSpan.Text;

                            // 标题
                            var title = element.FindElement(By.ClassName("bili-video-card__title"));
                            if (title == null) continue;
                            var titleText = title.GetAttribute("title") ?? string.Empty;

                            // 链接
                            var alink = title.FindElement(By.TagName("a"));
                            if (alink == null) continue;
                            var linkText = alink.GetAttribute("href") ?? string.Empty;

                            var info = new VideoInfo()
                            {
                                Author = up.Name,
                                CoverUrl = coverUrl,
                                PublishDate = dateText,
                                Title = titleText,
                                VideoUrl = linkText
                            };

                            list.Add(info);
                        }
                        catch (NoSuchElementException)
                        {
                            _logger.LogInformation($"Selenium resolve element error.");
                        }
                    }

                    _logger.LogInformation($"ResolveBiliBiliSpace from {up.Name}, total {list.Count} video(s).");

                    return list;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"ResolveBiliBiliSpace 发生错误: {ex.Message}");
                    return new List<VideoInfo>();
                }
            });
        }
        /// <summary>
        /// 解析Bilibili投稿日期并生成排序键
        /// </summary>
        /// <param name="publishDate"></param>
        /// <param name="now"></param>
        /// <returns></returns>
        private static (int Category, long Value) ParsePublishDate(string publishDate, DateTime now)
        {
            if (publishDate.EndsWith("小时前"))
            {
                // 提取小时数，类别0（最高优先级），值越小越靠前
                int hours = int.Parse(publishDate.Replace("小时前", "").Trim());
                return (0, hours);
            }
            else if (publishDate.EndsWith("天前"))
            {
                // 提取天数，类别1，值越小越靠前
                int days = int.Parse(publishDate.Replace("天前", "").Trim());
                return (1, days);
            }
            else
            {
                // 处理具体日期（月-日 或 年-月-日）
                DateTime date;
                if (DateTime.TryParseExact(publishDate, "MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    // 无年份：使用当前年份，并调整跨年情况
                    date = new DateTime(now.Year, date.Month, date.Day);
                    if (date > now) date = date.AddYears(-1); // 避免未来日期
                }
                else
                {
                    // 尝试解析年-月-日格式
                    DateTime.TryParseExact(publishDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
                }

                // 类别2（最低优先级），用Ticks的负值实现日期倒序（越近的日期Value越小）
                return (2, -date.Ticks);
            }
        }
        /// <summary>
        /// 清理所有Chrome进程（防止上次卡死遗留）
        /// </summary>
        public void KillAllChromeProcesses()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                KillProcessesByName("chrome");
                KillProcessesByName("chromedriver");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Linux 和 macOS
                KillProcessesByName("google-chrome");
                KillProcessesByName("chromedriver");

                // 额外使用命令行工具确保所有相关进程被终止
                ExecuteCommand("pkill", "-f chrome");
                ExecuteCommand("pkill", "-f chromedriver");
            }
        }
        private void KillProcessesByName(string processName)
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000); // 等待最多5秒
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"无法终止进程 {processName}: {ex.Message}");
                }
            }
        }
        private void ExecuteCommand(string command, string arguments)
        {
            try
            {
                using (var process = new Process())
                {
                    process.StartInfo.FileName = command;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.Start();
                    process.WaitForExit(5000); // 等待最多5秒
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"执行命令失败: {command} {arguments}, 错误: {ex.Message}");
            }
        }
    }
}
