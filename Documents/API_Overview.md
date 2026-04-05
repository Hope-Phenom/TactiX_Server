# TactiX Server API 总览

## 基本信息

| 项目 | 值 |
|------|-----|
| **Base URL (开发)** | `http://localhost:5000` |
| **Base URL (生产)** | 待配置 |
| **协议** | HTTP/HTTPS |
| **数据格式** | JSON |
| **字符编码** | UTF-8 |

---

## 认证方式

### JWT Bearer Token

所有需要认证的接口使用 JWT Bearer Token 认证：

```
Authorization: Bearer {access_token}
```

**获取Token流程：**
1. 调用 `/api/Auth/Login/{provider}` 获取OAuth登录URL
2. 用户完成OAuth授权后，回调 `/api/Auth/Callback/{provider}`
3. 回调响应中包含 `accessToken` 和 `refreshToken`

**Token有效期：**
- `accessToken`: 配置的过期时间（小时），响应中 `expiresIn` 为秒数
- `refreshToken`: 用于刷新accessToken（待实现）

**开发模式：**
- 调用 `/api/Auth/DevLogin?devUserId={userId}` 直接获取Token，无需OAuth流程

---

## 通用约定

### 请求格式

| 类型 | 说明 |
|------|------|
| `GET` | 参数通过Query String传递 |
| `POST` | 参数通过JSON Body或Form Data传递 |
| `DELETE` | 参数通过Path传递 |

### 分页参数

所有列表类接口支持统一分页参数：

| 参数 | 类型 | 默认值 | 范围 | 说明 |
|------|------|--------|------|------|
| `page` | int | 1 | 1-1000 | 页码（从1开始） |
| `pageSize` | int | 20 | 1-50 | 每页数量 |

### 分页响应格式

```json
{
  "totalCount": 100,
  "page": 1,
  "pageSize": 20,
  "items": [...]
}
```

### 日期格式

所有日期时间使用 ISO 8601 格式：
```
2024-01-15T10:30:00Z
```

### 数据类型

| 类型 | 说明 | JSON表示 |
|------|------|----------|
| `long` | 64位整数 | 数字 |
| `uint` | 无符号整数 | 数字 |
| `string?` | 可空字符串 | 字符串或null |
| `DateTime` | 日期时间 | ISO 8601字符串 |

---

## HTTP状态码

| 状态码 | 说明 | 场景 |
|--------|------|------|
| `200 OK` | 成功 | 请求处理成功 |
| `400 Bad Request` | 参数错误 | 无效参数、格式错误 |
| `401 Unauthorized` | 未认证 | Token缺失或过期 |
| `403 Forbidden` | 无权限 | 权限不足 |
| `404 Not Found` | 资源不存在 | 文件/用户不存在 |
| `500 Internal Server Error` | 服务器错误 | 内部异常 |

### 错误响应格式

```json
{
  "error": "错误描述信息"
}
```
或
```json
{
  "message": "错误描述信息"
}
```

---

## API模块概览

| 模块 | 路由前缀 | 说明 |
|------|----------|------|
| **认证** | `/api/Auth` | OAuth登录、Token获取、用户等级信息 |
| **战术大厅** | `/api/TacticsHall` | 文件上传/下载/搜索/审核 |
| **战术互动** | `/api/TacticsInteraction` | 点赞/收藏/评论 |
| **新闻** | `/api/News` | 社区新闻、系统公告 |
| **统计** | `/api/Stats` | 异常报告、版本控制 |

---

## 接口认证要求

| 认证级别 | 说明 | Header要求 |
|----------|------|------------|
| `[AllowAnonymous]` | 无需认证 | 无 |
| `[Authorize]` | 需要登录 | `Authorization: Bearer {token}` |
| `[Authorize] + Admin` | 需要管理员权限 | Token对应用户为管理员 |

---

## ShareCode (配装码) 格式

**详细定义见 [Constants.md](Constants.md#配装码-sharecode)**

8字符62进制编码，示例：`00000000`, `0000000z`, `zzzzzzzz`

---

## 文件上传约定

### 上传接口

- 使用 `multipart/form-data` 格式
- 文件字段名：`file`
- 最大文件大小：100MB（`RequestSizeLimit(104857600)`）

### 文件格式

- 扩展名：`.tactix`
- 内容格式：JSON
- 必需字段：`actions`（数组）

### 文件验证层次

**详细说明见 [FileFormat.md](FileFormat.md#验证层次)**

1. 结构验证 2. 大小验证 3. 内容验证 4. XSS验证 5. 哈希验证

---

## 用户等级权限矩阵

**完整定义见 [Constants.md](Constants.md#用户等级-userlevels)**

---

## 典型使用流程

### 1. 用户登录获取Token

```
GET /api/Auth/DevLogin?devUserId=test_user
→ Response: { "accessToken": "xxx", "userId": 1, ... }
```

### 2. 上传战术文件

```
POST /api/TacticsHall/Upload
Authorization: Bearer {token}
Content-Type: multipart/form-data
→ Response: { "shareCode": "00000000", "status": "pending" }
```

### 3. 等待审核（管理员操作）

```
POST /api/TacticsHall/Review/00000000
Authorization: Bearer {admin_token}
Body: { "approved": true }
→ Response: { "status": "approved" }
```

### 4. 用户浏览/下载

```
GET /api/TacticsHall/Detail/00000000
→ Response: { 文件详情 }

GET /api/TacticsHall/Download/00000000
→ Response: 文件流
```

### 5. 用户互动

```
POST /api/TacticsInteraction/Like/00000000
Authorization: Bearer {token}
→ Response: { "isLiked": true, "likeCount": 1 }

POST /api/TacticsInteraction/Comment/00000000
Authorization: Bearer {token}
Body: { "content": "很棒的战术！" }
→ Response: { "commentId": 1, ... }
```

---

## 相关文档

- [API_Auth.md](API_Auth.md) - 认证模块详细接口
- [API_TacticsHall.md](API_TacticsHall.md) - 战术大厅核心接口
- [API_TacticsInteraction.md](API_TacticsInteraction.md) - 战术互动接口
- [API_News.md](API_News.md) - 新闻模块接口
- [API_Stats.md](API_Stats.md) - 统计模块接口
- [DataModels.md](DataModels.md) - 数据模型定义
- [Constants.md](Constants.md) - 常量与权限矩阵
- [FileFormat.md](FileFormat.md) - .tactix文件格式规范
- [ErrorCodes.md](ErrorCodes.md) - 错误码定义