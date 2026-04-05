# 通知模块 API

**路由前缀：** `/api/Notification`

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/` | GET | 需认证 | 获取通知列表 |
| `/UnreadCount` | GET | 需认证 | 未读通知数量 |
| `/{id}/Read` | POST | 需认证 | 标记已读 |
| `/MarkAllRead` | POST | 需认证 | 全部标记已读 |
| `/{id}` | DELETE | 需认证 | 删除通知 |

---

## 1. 获取通知列表

### 基本信息

```http
GET /api/Notification
```

**认证要求：** 需要JWT Token

**用途：** 获取当前用户的通知列表。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `unreadOnly` | Query | bool | 否 | 仅未读通知，默认false |
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/Notification?unreadOnly=true&page=1&pageSize=20
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 响应结构

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "notifications": [
    {
      "id": "long",
      "type": "string",
      "typeDisplay": "string",
      "title": "string",
      "content": "string?",
      "relatedShareCode": "string?",
      "relatedFileName": "string?",
      "relatedUserNickname": "string?",
      "isRead": "bool",
      "createdAt": "DateTime",
      "timeDisplay": "string?"
    }
  ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `type` | string | 通知类型代码 |
| `typeDisplay` | string | 通知类型显示名称 |
| `title` | string | 通知标题 |
| `content` | string? | 通知内容 |
| `relatedShareCode` | string? | 关联文件配装码 |
| `relatedFileName` | string? | 关联文件名称 |
| `relatedUserNickname` | string? | 触发通知用户昵称 |
| `timeDisplay` | string? | 时间显示（如"5分钟前"） |

### 响应示例

```json
{
  "totalCount": 15,
  "page": 1,
  "pageSize": 20,
  "notifications": [
    {
      "id": 1,
      "type": "file_approved",
      "typeDisplay": "审核通过",
      "title": "文件审核通过",
      "content": "您的战术文件「PvZ 4Gate Rush」已通过审核，现在可以公开访问了。",
      "relatedShareCode": "00000001",
      "relatedFileName": "PvZ 4Gate Rush",
      "relatedUserNickname": null,
      "isRead": false,
      "createdAt": "2024-03-15T10:30:00Z",
      "timeDisplay": "5分钟前"
    },
    {
      "id": 2,
      "type": "comment_reply",
      "typeDisplay": "评论回复",
      "title": "有人回复了你的评论",
      "content": "在战术文件「TvZ 3Rax」中，有人回复了你的评论。",
      "relatedShareCode": "00000002",
      "relatedFileName": "TvZ 3Rax",
      "relatedUserNickname": "SC2Player",
      "isRead": true,
      "createdAt": "2024-03-15T09:00:00Z",
      "timeDisplay": "2小时前"
    }
  ]
}
```

---

## 2. 获取未读通知数量

### 基本信息

```http
GET /api/Notification/UnreadCount
```

**认证要求：** 需要JWT Token

**用途：** 获取当前用户的未读通知数量，用于显示角标。

### 请求示例

```http
GET /api/Notification/UnreadCount
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 响应结构

```json
{
  "unreadCount": "int"
}
```

### 响应示例

```json
{
  "unreadCount": 5
}
```

---

## 3. 标记通知已读

### 基本信息

```http
POST /api/Notification/{id}/Read
```

**认证要求：** 需要JWT Token

**用途：** 标记单个通知为已读。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `id` | Path | long | 是 | 通知ID |

### 请求示例

```http
POST /api/Notification/1/Read
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 响应示例

```json
{
  "message": "已标记为已读"
}
```

---

## 4. 标记所有通知已读

### 基本信息

```http
POST /api/Notification/MarkAllRead
```

**认证要求：** 需要JWT Token

**用途：** 将当前用户所有未读通知标记为已读。

### 请求示例

```http
POST /api/Notification/MarkAllRead
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 响应示例

```json
{
  "message": "全部已标记为已读"
}
```

---

## 5. 删除通知

### 基本信息

```http
DELETE /api/Notification/{id}
```

**认证要求：** 需要JWT Token

**用途：** 删除单个通知。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `id` | Path | long | 是 | 通知ID |

### 响应示例

```json
{
  "message": "通知已删除"
}
```

---

## 通知类型

| 类型代码 | 显示名称 | 触发场景 |
|----------|----------|----------|
| `file_approved` | 审核通过 | 用户上传的文件通过审核 |
| `file_rejected` | 审核拒绝 | 用户上传的文件被拒绝 |
| `comment_reply` | 评论回复 | 用户的评论被回复 |
| `file_liked` | 获得点赞 | 用户的文件被点赞 |
| `report_processed` | 举报处理 | 用户提交的举报被处理 |

---

## 通知触发流程

```
审核通知:
TacticsHallController.Review() 
  → 审核完成 
  → TacticsNotificationService.CreateNotificationAsync()

评论回复通知:
TacticsInteractionService.AddCommentAsync() 
  → 评论保存成功 
  → TacticsNotificationService.CreateNotificationAsync()

点赞通知:
TacticsInteractionService.ToggleLikeAsync() 
  → 点赞成功 
  → TacticsNotificationService.CreateNotificationAsync()
```

---

## 时间显示规则

| 时间差 | 显示文本 |
|--------|----------|
| < 1分钟 | "刚刚" |
| < 1小时 | "X分钟前" |
| < 24小时 | "X小时前" |
| < 7天 | "X天前" |
| >= 7天 | "MM-dd HH:mm" |

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定
- [Constants.md](Constants.md) - 通知类型定义