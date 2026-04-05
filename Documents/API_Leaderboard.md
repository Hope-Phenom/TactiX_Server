# 排行榜模块 API

**路由前缀：** `/api/Leaderboard`

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/HotFiles` | GET | 无需认证 | 热门战术排行榜 |
| `/TopUploaders` | GET | 无需认证 | 贡献者排行榜 |

---

## 1. 热门战术排行榜

### 基本信息

```http
GET /api/Leaderboard/HotFiles
```

**认证要求：** 无需认证

**用途：** 获取热门战术文件排行榜。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `period` | Query | string | 否 | 时间周期，默认weekly |
| `race` | Query | string | 否 | 种族筛选：P/T/Z |
| `sortBy` | Query | string | 否 | 排序方式，默认downloads |
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 时间周期参数

| 值 | 说明 |
|------|------|
| `daily` | 最近24小时 |
| `weekly` | 最近7天 |
| `monthly` | 最近30天 |
| `all` | 全部时间 |

### 排序方式参数

| 值 | 说明 |
|------|------|
| `downloads` | 按下载量排序（默认） |
| `likes` | 按点赞数排序 |

### 请求示例

```http
GET /api/Leaderboard/HotFiles?period=weekly&race=P&sortBy=downloads&page=1&pageSize=10
```

### 响应结构

```json
{
  "period": "string",
  "race": "string?",
  "sortBy": "string",
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "files": [
    {
      "rank": "int",
      "shareCode": "string",
      "name": "string?",
      "author": "string?",
      "race": "string?",
      "raceDisplay": "string?",
      "downloadCount": "uint",
      "likeCount": "uint",
      "favoriteCount": "uint",
      "createdAt": "DateTime",
      "uploader": {
        "id": "long",
        "nickname": "string?",
        "avatarUrl": "string?",
        "levelCode": "string?"
      }
    }
  ]
}
```

### 响应示例

```json
{
  "period": "weekly",
  "race": "P",
  "sortBy": "downloads",
  "totalCount": 50,
  "page": 1,
  "pageSize": 10,
  "files": [
    {
      "rank": 1,
      "shareCode": "00000001",
      "name": "PvZ 4Gate Rush",
      "author": "ProtossMaster",
      "race": "P",
      "raceDisplay": "神族",
      "downloadCount": 1520,
      "likeCount": 320,
      "favoriteCount": 85,
      "createdAt": "2024-03-10T08:00:00Z",
      "uploader": {
        "id": 100,
        "nickname": "ProtossMaster",
        "avatarUrl": "https://example.com/avatar.jpg",
        "levelCode": "pro"
      }
    }
  ]
}
```

---

## 2. 贡献者排行榜

### 基本信息

```http
GET /api/Leaderboard/TopUploaders
```

**认证要求：** 无需认证

**用途：** 获取上传者贡献排行榜。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `period` | Query | string | 否 | 时间周期，默认monthly |
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/Leaderboard/TopUploaders?period=monthly&page=1&pageSize=10
```

### 响应结构

```json
{
  "period": "string",
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "uploaders": [
    {
      "rank": "int",
      "userId": "long",
      "nickname": "string?",
      "avatarUrl": "string?",
      "levelCode": "string?",
      "levelName": "string?",
      "uploadCount": "int",
      "totalDownloadCount": "uint",
      "totalLikeCount": "uint",
      "qualityScore": "double"
    }
  ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `uploadCount` | int | 上传文件数量 |
| `totalDownloadCount` | uint | 总下载次数 |
| `totalLikeCount` | uint | 总点赞次数 |
| `qualityScore` | double | 综合质量评分 |

### 响应示例

```json
{
  "period": "monthly",
  "totalCount": 100,
  "page": 1,
  "pageSize": 10,
  "uploaders": [
    {
      "rank": 1,
      "userId": 100,
      "nickname": "ProtossMaster",
      "avatarUrl": "https://example.com/avatar.jpg",
      "levelCode": "pro",
      "levelName": "职业选手",
      "uploadCount": 25,
      "totalDownloadCount": 15000,
      "totalLikeCount": 3500,
      "qualityScore": 6950.0
    }
  ]
}
```

---

## 质量评分计算

```
qualityScore = totalDownloadCount × 0.3 + totalLikeCount × 0.7
```

评分权重：
- 下载量权重：30%
- 点赞数权重：70%

---

## 数据说明

### 筛选规则

- 仅显示已审核通过（approved）的文件
- 不包含已删除的文件
- 种族筛选使用大写字母（P/T/Z）

### 排序规则

**热门战术：**
- 按下载量：下载量降序 → 点赞数降序
- 按点赞数：点赞数降序 → 下载量降序

**贡献者：**
- 上传数量降序 → 总下载量降序

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定
- [Constants.md](Constants.md) - 种族代码定义