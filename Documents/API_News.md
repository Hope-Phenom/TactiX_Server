# 新闻模块 API

**路由前缀：** `/api/News`

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/GetNews` | GET | 无 | 获取社区新闻 |
| `/GetNewsSys` | GET | 无 | 获取系统公告 |

---

## 1. 获取社区新闻

### 基本信息

```
GET /api/News/GetNews
```

**认证要求：** 无

**用途：** 获取社区新闻列表（包含scboy论坛热帖和B站推荐视频）。

### 请求参数

无

### 请求示例

```http
GET /api/News/GetNews
```

### 响应结构

```json
[
  {
    "id": "int",
    "title": "string",
    "link": "string",
    "dateTime": "DateTime",
    "type": "int"
  }
]
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | int | 新闻ID |
| `title` | string | 新闻标题 |
| `link` | string | 新闻链接 |
| `dateTime` | DateTime | 发布时间 |
| `type` | int | 类型：0=论坛帖子，1=B站视频 |

### 响应示例

```json
[
  {
    "id": 1001,
    "title": "星际争霸2 1.4版本更新说明",
    "link": "https://www.scboy.cc/thread/12345",
    "dateTime": "2024-03-15T10:00:00",
    "type": 0
  },
  {
    "id": 1002,
    "title": "【视频】神族开局教学：如何应对虫族快攻",
    "link": "https://www.bilibili.com/video/BV1xx411c7mD",
    "dateTime": "2024-03-14T18:30:00",
    "type": 1
  }
]
```

### 数据来源

- **Type 0 (论坛)**：scboy.cc 论坛热帖（后台服务定时抓取）
- **Type 1 (B站)**：指定UP主的最新视频（后台服务定时抓取）

---

## 2. 获取系统公告

### 基本信息

```
GET /api/News/GetNewsSys
```

**认证要求：** 无

**用途：** 获取系统公告（最新5条）。

### 请求参数

无

### 请求示例

```http
GET /api/News/GetNewsSys
```

### 响应结构

```json
[
  {
    "id": "int",
    "title": "string",
    "link": "string",
    "dateTime": "DateTime"
  }
]
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | int | 公告ID |
| `title` | string | 公告标题 |
| `link` | string | 公告链接 |
| `dateTime` | DateTime | 发布时间 |

### 响应示例

```json
[
  {
    "id": 101,
    "title": "TactiX 2.0版本发布",
    "link": "https://tactix.com/blog/v2.0",
    "dateTime": "2024-03-01T00:00:00"
  },
  {
    "id": 100,
    "title": "服务器维护通知",
    "link": "https://tactix.com/blog/maintenance",
    "dateTime": "2024-02-28T00:00:00"
  }
]
```

---

## 后台服务说明

新闻数据由 `NewsGenerateService` 后台服务定时更新：

- **启动延迟**：30秒
- **更新间隔**：每2小时
- **数据源**：
  - scboy.cc 论坛（Selenium抓取）
  - B站UP主视频（API抓取）

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定