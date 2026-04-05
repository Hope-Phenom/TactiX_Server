# 战术互动模块 API

**路由前缀：** `/api/TacticsInteraction`

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/Like/{shareCode}` | POST | 需认证 | 点赞/取消点赞 |
| `/Favorite/{shareCode}` | POST | 需认证 | 收藏/取消收藏 |
| `/Liked` | GET | 需认证 | 获取我点赞的文件 |
| `/Favorites` | GET | 需认证 | 获取我的收藏 |
| `/Comment/{shareCode}` | POST | 需认证 | 添加评论 |
| `/Comments/{shareCode}` | GET | 无 | 获取文件评论 |
| `/Comment/{id}` | DELETE | 需认证 | 删除评论 |

---

## 1. 点赞/取消点赞

### 基本信息

```
POST /api/TacticsInteraction/Like/{shareCode}
```

**认证要求：** 需要JWT Token

**用途：** Toggle模式——点赞或取消点赞，返回操作后的最终状态。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |

### 请求示例

```http
POST /api/TacticsInteraction/Like/000000AB
Authorization: Bearer {token}
```

### 响应结构

```json
{
  "isLiked": "bool",
  "likeCount": "uint"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `isLiked` | bool | 操作后的最终状态（true=已点赞，false=已取消） |
| `likeCount` | uint | 更新后的点赞总数 |

### 响应示例

**点赞成功：**
```json
{
  "isLiked": true,
  "likeCount": 43
}
```

**取消点赞：**
```json
{
  "isLiked": false,
  "likeCount": 42
}
```

### 数据流说明

```
1. 解码 shareCode → fileId
2. 检查文件存在性和审核状态（必须 approved）
3. 检查用户 CanComment 权限
4. 使用事务执行 Toggle 操作：
   ┌─────────────────────────────────────┐
   │ 已点赞 → 删除记录，likeCount - 1    │
   │ 未点赞 → 创建记录，likeCount + 1    │
   └─────────────────────────────────────┘
5. 返回最终状态和计数
```

### 权限要求

- 用户等级 `CanComment = true`
- 文件必须为 `approved` 状态

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 400 | 无效配装码 | `{ "error": "无效的配装码格式" }` |
| 401 | 未登录 | `{ "error": "未登录或登录已过期" }` |
| 400 | 文件不存在 | `{ "error": "操作失败，文件不存在或无权限" }` |
| 400 | 文件未审核 | `{ "isLiked": false, "likeCount": 0 }` |

---

## 2. 收藏/取消收藏

### 基本信息

```
POST /api/TacticsInteraction/Favorite/{shareCode}
```

**认证要求：** 需要JWT Token

**用途：** Toggle模式——收藏或取消收藏。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |

### 请求示例

```http
POST /api/TacticsInteraction/Favorite/000000AB
Authorization: Bearer {token}
```

### 响应结构

```json
{
  "isFavorited": "bool",
  "favoriteCount": "uint"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `isFavorited` | bool | 操作后的最终状态 |
| `favoriteCount` | uint | 更新后的收藏总数 |

### 响应示例

```json
{
  "isFavorited": true,
  "favoriteCount": 16
}
```

### 权限要求

- 已登录用户即可收藏
- 文件必须为 `approved` 状态

### 错误响应

同点赞接口。

---

## 3. 获取我点赞的文件列表

### 基本信息

```
GET /api/TacticsInteraction/Liked
```

**认证要求：** 需要JWT Token

**用途：** 获取当前用户点赞过的所有文件列表。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/TacticsInteraction/Liked?page=1&pageSize=10
Authorization: Bearer {token}
```

### 响应结构

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "files": [
    {
      "shareCode": "string",
      "name": "string?",
      "author": "string?",
      "race": "string?",
      "raceDisplay": "string?",
      "downloadCount": "uint",
      "likeCount": "uint",
      "currentVersion": "int",
      "createdAt": "DateTime"
    }
  ]
}
```

### 响应示例

```json
{
  "totalCount": 15,
  "page": 1,
  "pageSize": 10,
  "files": [
    {
      "shareCode": "000000AB",
      "name": "PvZ开局战术",
      "author": "ProtossMaster",
      "race": "P",
      "raceDisplay": "神族",
      "downloadCount": 150,
      "likeCount": 43,
      "currentVersion": 3,
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ]
}
```

---

## 4. 获取我的收藏列表

### 基本信息

```
GET /api/TacticsInteraction/Favorites
```

**认证要求：** 需要JWT Token

**用途：** 获取当前用户收藏的所有文件列表。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/TacticsInteraction/Favorites?page=1&pageSize=20
Authorization: Bearer {token}
```

### 响应结构

同 `/Liked` 响应结构。

---

## 5. 添加评论

### 基本信息

```
POST /api/TacticsInteraction/Comment/{shareCode}
```

**认证要求：** 需要JWT Token

**用途：** 为文件添加评论，支持嵌套回复（最多2层）。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |

### 请求体

```json
{
  "content": "string",
  "parentCommentId": "long?"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `content` | string | 是 | 评论内容，1-1000字符 |
| `parentCommentId` | long? | 否 | 父评论ID（用于回复） |

### 请求示例

**顶级评论：**
```http
POST /api/TacticsInteraction/Comment/000000AB
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "这个战术很有用！"
}
```

**回复评论：**
```http
POST /api/TacticsInteraction/Comment/000000AB
Authorization: Bearer {token}
Content-Type: application/json

{
  "content": "同意，我也在用这套开局",
  "parentCommentId": 123
}
```

### 响应结构

```json
{
  "commentId": "long",
  "content": "string",
  "createdAt": "DateTime",
  "parentCommentId": "long?"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `commentId` | long | 新创建的评论ID |
| `content` | string | 评论内容（已XSS过滤） |
| `createdAt` | DateTime | 创建时间 |
| `parentCommentId` | long? | 父评论ID |

### 响应示例

```json
{
  "commentId": 456,
  "content": "这个战术很有用！",
  "createdAt": "2024-03-15T14:30:00Z",
  "parentCommentId": null
}
```

### 数据流说明

```
1. 解码 shareCode → fileId
2. 检查文件存在性和审核状态
3. 检查用户 CanComment 权限
4. XSS过滤：HtmlEncode(content)
5. 如果有 parentCommentId：
   ├─ 检查父评论存在
   ├─ 验证父评论属于同一文件
   └─ 限制嵌套深度≤2层（父评论不能有ParentCommentId）
6. 创建评论记录 → 返回结果
```

### 嵌套评论规则

```
顶级评论 (ParentCommentId = null)
└── 回复 (ParentCommentId = 顶级评论ID)
    └── ❌ 不能再回复（超过2层限制）
```

### 权限要求

- 用户等级 `CanComment = true`
- 文件必须为 `approved` 状态

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 400 | 内容为空或过长 | `{ "error": "评论内容长度必须在1-1000字符之间" }` |
| 400 | 父评论不存在 | `{ "error": "评论失败，文件不存在或无权限" }` |
| 400 | 嵌套过深 | `{ "error": "评论失败，文件不存在或无权限" }` |

---

## 6. 获取文件评论列表

### 基本信息

```
GET /api/TacticsInteraction/Comments/{shareCode}
```

**认证要求：** 无

**用途：** 获取文件的所有评论（含嵌套回复）。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/TacticsInteraction/Comments/000000AB?page=1&pageSize=20
```

### 响应结构

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "comments": [
    {
      "id": "long",
      "content": "string",
      "isDeleted": "bool",
      "createdAt": "DateTime",
      "updatedAt": "DateTime?",
      "author": {
        "id": "long",
        "nickname": "string?",
        "avatarUrl": "string?",
        "levelCode": "string?",
        "levelName": "string?"
      },
      "parentCommentId": "long?",
      "replies": []
    }
  ]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `totalCount` | int | 顶级评论总数 |
| `comments` | array | 顶级评论列表 |
| `comments[].id` | long | 评论ID |
| `comments[].content` | string | 内容（已删除显示"[已删除]"） |
| `comments[].isDeleted` | bool | 是否已删除 |
| `comments[].author` | object | 作者信息（已删除为null） |
| `comments[].replies` | array | 子回复列表 |

### 响应示例

```json
{
  "totalCount": 5,
  "page": 1,
  "pageSize": 20,
  "comments": [
    {
      "id": 123,
      "content": "这个战术很有用！",
      "isDeleted": false,
      "createdAt": "2024-03-15T14:30:00Z",
      "updatedAt": null,
      "author": {
        "id": 10001,
        "nickname": "SC2Player",
        "avatarUrl": "https://example.com/avatar.jpg",
        "levelCode": "verified",
        "levelName": "认证作者"
      },
      "parentCommentId": null,
      "replies": [
        {
          "id": 124,
          "content": "同意，我也在用这套开局",
          "isDeleted": false,
          "createdAt": "2024-03-15T15:00:00Z",
          "updatedAt": null,
          "author": {
            "id": 10002,
            "nickname": "AnotherPlayer",
            "avatarUrl": null,
            "levelCode": "normal",
            "levelName": "普通用户"
          },
          "parentCommentId": 123,
          "replies": []
        }
      ]
    },
    {
      "id": 125,
      "content": "[已删除]",
      "isDeleted": true,
      "createdAt": "2024-03-16T10:00:00Z",
      "updatedAt": "2024-03-17T09:00:00Z",
      "author": null,
      "parentCommentId": null,
      "replies": []
    }
  ]
}
```

### 软删除说明

- 删除评论时设置 `isDeleted = true`
- 已删除评论内容显示为 `"[已删除]"`
- 已删除评论的 `author` 返回 `null`

---

## 7. 删除评论

### 基本信息

```
DELETE /api/TacticsInteraction/Comment/{id}
```

**认证要求：** 需要JWT Token

**用途：** 软删除评论（标记为已删除）。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `id` | Path | long | 是 | 评论ID |

### 请求示例

```http
DELETE /api/TacticsInteraction/Comment/123
Authorization: Bearer {token}
```

### 响应结构

```json
{
  "message": "string"
}
```

### 响应示例

```json
{
  "message": "删除成功"
}
```

### 权限说明

- **普通用户**：只能删除自己的评论
- **管理员**：可删除任何评论

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 401 | 未登录 | `{ "error": "未登录或登录已过期" }` |
| 400 | 无权限 | `{ "error": "只能删除自己的评论" }` |
| 400 | 评论不存在 | `{ "error": "评论不存在" }` |

---

## 数据流总览

```
┌─────────────────────────────────────────────────────────────┐
│                     战术文件 (approved)                      │
└─────────────────────────────────────────────────────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         │                    │                    │
         ▼                    ▼                    ▼
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│    点赞     │      │    收藏     │      │    评论     │
│  (Toggle)   │      │  (Toggle)   │      │  (CRUD)     │
└─────────────┘      └─────────────┘      └─────────────┘
         │                    │                    │
         │                    │                    ▼
         │                    │           ┌─────────────┐
         │                    │           │  嵌套回复   │
         │                    │           │  (最多2层)  │
         │                    │           └─────────────┘
         ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────┐
│                     用户互动记录                             │
│  - tactics_like (点赞记录)                                  │
│  - tactics_favorite (收藏记录)                              │
│  - tactics_comment (评论记录)                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定
- [API_TacticsHall.md](API_TacticsHall.md) - 战术大厅核心接口
- [Constants.md](Constants.md) - 用户等级权限矩阵