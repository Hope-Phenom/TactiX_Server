# TactiX_Server

## 项目简介
TactiX_Server 是一个基于 ASP.NET Core 8.0 开发的 Web API 服务器，主要用于提供新闻管理、统计数据收集和版本控制等功能。

## 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 开发框架 |
| ASP.NET Core | 8.0 | Web API |
| Entity Framework Core | 9.0.8 | ORM 框架 |
| MySQL | - | 数据库 |
| Pomelo.EntityFrameworkCore.MySql | 9.0.0 | MySQL 数据库驱动 |
| NLog | 6.0.5 | 日志记录 |
| Swagger | 6.6.2 | API 文档 |
| Selenium | 4.38.0 | 网页爬取 |
| AngleSharp | 1.3.0 | HTML 解析 |
| Docker | - | 容器化部署 |

## 项目结构

```
TactiX_Server/
├── SQLScript/                 # 数据库脚本
│   └── tactix_database.sql    # 数据库初始化脚本
├── TactiX_Server/             # 主项目目录
│   ├── Controllers/           # API 控制器
│   │   ├── NewsController.cs  # 新闻相关接口
│   │   └── StatsController.cs # 统计相关接口
│   ├── Data/                  # 数据库上下文
│   │   ├── NewsDbContext.cs   # 新闻数据库上下文
│   │   └── StatsDbContext.cs  # 统计数据库上下文
│   ├── Models/                # 数据模型
│   │   ├── News/              # 新闻相关模型
│   │   ├── Req/               # 请求模型
│   │   ├── Resp/              # 响应模型
│   │   └── Stats/             # 统计相关模型
│   ├── Service/               # 业务逻辑服务
│   │   └── NewsGenerateService.cs # 新闻生成服务
│   ├── Dockerfile             # Docker 配置文件
│   ├── Program.cs             # 程序入口
│   ├── ServerConfig.cs        # 服务器配置
│   ├── TactiX_Server.csproj   # 项目配置
│   ├── appsettings.json       # 应用配置
│   └── nlog.config            # 日志配置
├── .dockerignore              # Docker 忽略文件
├── .gitignore                 # Git 忽略文件
├── CONFIGURE.md               # 配置说明文档
├── LICENSE.txt                # 许可证
└── README.md                  # 项目说明文档
```

## 快速开始

### 环境要求
- .NET 8.0 SDK
- MySQL 数据库
- Chrome 浏览器（用于 Selenium 爬取）

### 安装步骤

1. 克隆仓库
```bash
git clone <repository-url>
cd TactiX_Server
```

2. 配置数据库
   - 创建数据库并执行 `SQLScript/tactix_database.sql` 初始化表结构
   - 配置环境变量 `TACTIX_CONNCTION_STRINGS` 为数据库连接字符串

3. 配置其他环境变量
   ```bash
   # 论坛用户名
   TACTIX_FORUM_USERNAME=<forum-username>
   
   # 论坛密码
   TACTIX_FORUM_PASSWORD=<forum-password>
   
   # 数据库连接字符串
   TACTIX_CONNCTION_STRINGS=<database-connection-string>
   ```

4. 运行项目
```bash
dotnet run --project TactiX_Server/TactiX_Server.csproj
```

5. 访问 API 文档
   - 开发环境：http://localhost:5000/swagger

## 配置说明

### 应用配置
- `appsettings.json`：主配置文件
- `appsettings.Development.json`：开发环境配置
- 环境变量：用于敏感信息配置

### 日志配置
- `nlog.config`：NLog 日志配置文件，支持多种日志目标

### 数据库配置
- 支持 MySQL 数据库
- 使用 Entity Framework Core 进行数据库操作

## API 接口文档

### 新闻相关接口

#### GET /api/News/GetNews
获取最新新闻列表

**响应示例**：
```json
[
  {
    "Id": 1,
    "Title": "新闻标题",
    "Content": "新闻内容",
    "Type": 0,
    "Update_DateTime": "2024-01-26T12:00:00",
    "Create_DateTime": "2024-01-26T12:00:00"
  }
]
```

#### GET /api/News/GetNewsSys
获取系统新闻（最新5条）

**响应示例**：
```json
[
  {
    "Id": 1,
    "Title": "系统公告",
    "Content": "公告内容",
    "DateTime": "2024-01-26T12:00:00"
  }
]
```

### 统计相关接口

#### POST /api/Stats/PostExceptionReport
提交异常报告

**请求示例**：
```json
{
  "Id": 0,
  "Content": "异常内容",
  "StackTrace": "异常堆栈",
  "DateTime": "2024-01-26T12:00:00",
  "Version": "1.0.0",
  "DeviceInfo": "设备信息"
}
```

**响应**：
- 200 OK：保存成功
- 400 Bad Request：无效请求
- 500 Internal Server Error：服务器错误

#### POST /api/Stats/PostVersionControl
版本控制检查

**请求示例**：
```json
{
  "Version": "1.0.0"
}
```

**响应示例**：
```json
{
  "LastestVersion": "1.0.1",
  "Banned": false,
  "ForceUpgrade": false,
  "ReleaseUrl": "https://example.com/update"
}
```

## 数据库说明

### 主要表结构

1. **stats_excption_report**：异常报告表
2. **stats_version_control**：版本控制表
3. **news_community**：社区新闻表
4. **config_video_up**：视频UP主配置表
5. **news_sys**：系统新闻表

### 初始化脚本
- `SQLScript/tactix_database.sql`：包含所有表的创建语句

## 开发说明

### 代码风格
- 遵循 C# 编码规范
- 使用 nullable 引用类型
- 启用隐式 using 指令

### 日志记录
- 使用 NLog 进行日志记录
- 支持控制台、文件等多种日志目标

### 测试
- 项目包含 HTTP 请求测试文件 `TactiX_Server.http`
- 可使用 Visual Studio 或 Rider 直接运行测试

## Docker 部署

### 构建镜像
```bash
docker build -t tactix-server -f TactiX_Server/Dockerfile .
```

### 运行容器
```bash
docker run -d \
  --name tactix-server \
  -p 80:80 \
  -e TACTIX_CONNCTION_STRINGS=<database-connection-string> \
  -e TACTIX_FORUM_USERNAME=<forum-username> \
  -e TACTIX_FORUM_PASSWORD=<forum-password> \
  tactix-server
```

## 许可证

查看 `LICENSE.txt` 文件了解许可证信息。

## 贡献

欢迎提交 Issue 和 Pull Request。

## 联系方式

如有问题，请联系项目维护者。