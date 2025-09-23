using AngleSharp.Html.Parser;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net;
using TactiX_Server.Models;

namespace TactiX_Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpiderController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SpiderController> _logger;
        private readonly ServerConfig _options;

        public SpiderController(
            IHttpClientFactory httpClientFactory,
            ILogger<SpiderController> logger,
            IOptions<ServerConfig> options)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _options = options.Value;
        }

        [HttpGet("GetForumPosts")]
        public async Task<IActionResult> GetForumPosts()
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
                    return StatusCode(500, "Login failed");
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
                            Url = url,
                            Date = date
                        };

                        posts.Add(post);
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但继续处理其他帖子
                        Console.WriteLine($"解析帖子时出错: {ex.Message}");
                    }
                }

                return Ok(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching forum posts");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}