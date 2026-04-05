# 数据模型定义

本文档定义所有API使用的Request和Response模型。

---

## Request 模型

### UploadTacticsRequest

上传战术文件请求（除文件外的额外参数）。

```json
{
  "changelog": "string?"
}
```

| 字段 | 类型 | 必需 | 约束 | 说明 |
|------|------|------|------|------|
| `changelog` | string | 否 | 最长1000字符 | 版本更新说明 |

---

### UploadVersionRequest

上传新版本请求。

```json
{
  "shareCode": "string",
  "changelog": "string?"
}
```

| 字段 | 类型 | 必需 | 约束 | 说明 |
|------|------|------|------|------|
| `shareCode` | string | 是 | 固定8字符 | 配装码 |
| `changelog` | string | 否 | 最长1000字符 | 版本更新说明 |

---

### SearchTacticsRequest

搜索战术文件请求。

```json
{
  "keyword": "string?",
  "race": "string?",
  "uploaderId": "long?",
  "sortBy": "string",
  "page": "int",
  "pageSize": "int"
}
```

| 字段 | 类型 | 必需 | 约束 | 说明 |
|------|------|------|------|------|
| `keyword` | string | 否 | 最长100字符 | 关键词搜索 |
| `race` | string | 否 | P/T/Z | 种族筛选 |
| `uploaderId` | long | 否 | - | 上传者ID |
| `sortBy` | string | 否 | latest/popular/downloads | 排序方式，默认latest |
| `page` | int | 否 | 1-1000 | 页码，默认1 |
| `pageSize` | int | 否 | 1-50 | 每页数量，默认20 |

---

### ReviewRequest

审核请求（管理员）。

```json
{
  "approved": "bool"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `approved` | bool | 是 | true=通过，false=拒绝 |

---

### BatchReviewRequest

批量审核请求（管理员）。

```json
{
  "shareCodes": ["string"],
  "approved": "bool"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `shareCodes` | string[] | 是 | 配装码列表 |
| `approved` | bool | 是 | true=全部通过，false=全部拒绝 |

---

### AddCommentRequest

添加评论请求。

```json
{
  "content": "string",
  "parentCommentId": "long?"
}
```

| 字段 | 类型 | 必需 | 约束 | 说明 |
|------|------|------|------|------|
| `content` | string | 是 | 1-1000字符 | 评论内容 |
| `parentCommentId` | long | 否 | - | 父评论ID（用于回复） |

---

## Response 模型

### UploadTacticsResponse

上传响应。

```json
{
  "shareCode": "string",
  "fileId": "long",
  "versionNumber": "int",
  "status": "string",
  "message": "string"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `shareCode` | string | 8字符配装码 |
| `fileId` | long | 文件唯一ID |
| `versionNumber` | int | 版本号 |
| `status` | string | 状态：pending/approved/rejected |
| `message` | string | 提示消息 |

---

### TacticsDetailResponse

战术文件详情响应。

```json
{
  "shareCode": "string",
  "name": "string?",
  "author": "string?",
  "race": "string?",
  "raceDisplay": "string?",
  "fileSize": "long",
  "downloadCount": "uint",
  "likeCount": "uint",
  "favoriteCount": "uint",
  "isLikedByUser": "bool",
  "isFavoritedByUser": "bool",
  "currentVersion": "int",
  "status": "string",
  "createdAt": "DateTime",
  "updatedAt": "DateTime",
  "uploader": "UserBriefResponse?"
}
```

---

### UserBriefResponse

用户简要信息。

```json
{
  "id": "long",
  "nickname": "string?",
  "avatarUrl": "string?",
  "levelCode": "string?",
  "levelName": "string?"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 用户ID |
| `nickname` | string? | 昵称 |
| `avatarUrl` | string? | 头像URL |
| `levelCode` | string? | 等级代码（normal/verified/pro/admin） |
| `levelName` | string? | 等级名称 |

---

### SearchTacticsResponse

搜索结果响应。

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "files": ["TacticsBriefResponse"]
}
```

---

### TacticsBriefResponse

战术文件简要信息。

```json
{
  "shareCode": "string",
  "name": "string?",
  "author": "string?",
  "race": "string?",
  "raceDisplay": "string?",
  "downloadCount": "uint",
  "likeCount": "uint",
  "currentVersion": "int",
  "createdAt": "DateTime",
  "uploaderNickname": "string?"
}
```

---

### TacticsVersionListResponse

版本列表响应。

```json
{
  "shareCode": "string",
  "versions": ["TacticsVersionResponse"]
}
```

---

### TacticsVersionResponse

版本信息。

```json
{
  "versionNumber": "int",
  "fileSize": "long",
  "changelog": "string?",
  "createdAt": "DateTime"
}
```

---

### LikeToggleResponse

点赞Toggle响应。

```json
{
  "isLiked": "bool",
  "likeCount": "uint"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `isLiked` | bool | 操作后最终状态 |
| `likeCount` | uint | 更新后点赞总数 |

---

### FavoriteToggleResponse

收藏Toggle响应。

```json
{
  "isFavorited": "bool",
  "favoriteCount": "uint"
}
```

---

### UserInteractionListResponse

用户互动列表响应（点赞/收藏的文件列表）。

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "files": ["TacticsBriefResponse"]
}
```

---

### CommentListResponse

评论列表响应。

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "comments": ["CommentResponse"]
}
```

---

### CommentResponse

单条评论响应。

```json
{
  "id": "long",
  "content": "string",
  "isDeleted": "bool",
  "createdAt": "DateTime",
  "updatedAt": "DateTime?",
  "author": "UserBriefResponse?",
  "parentCommentId": "long?",
  "replies": ["CommentResponse"]
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 评论ID |
| `content` | string | 内容（已删除显示"[已删除]"） |
| `isDeleted` | bool | 是否已删除 |
| `createdAt` | DateTime | 创建时间 |
| `updatedAt` | DateTime? | 更新时间 |
| `author` | UserBriefResponse? | 作者信息（已删除为null） |
| `parentCommentId` | long? | 父评论ID |
| `replies` | array | 子回复列表 |

---

### AddCommentResponse

添加评论响应。

```json
{
  "commentId": "long",
  "content": "string",
  "createdAt": "DateTime",
  "parentCommentId": "long?"
}
```

---

## 数据库实体模型

### TacticsFileModel

战术文件主表（`tactics_file`）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 主键 |
| `shareCode` | string | 8字符配装码（唯一） |
| `name` | string? | 战术名称 |
| `author` | string? | 作者名称 |
| `race` | string? | 种族代码（P/T/Z） |
| `uploaderId` | long | 上传用户ID |
| `filePath` | string | 文件存储路径 |
| `fileHash` | string | SHA256哈希 |
| `fileSize` | long | 文件大小（字节） |
| `version` | int | 当前版本号 |
| `status` | string | 审核状态 |
| `downloadCount` | uint | 下载次数 |
| `likeCount` | uint | 点赞次数 |
| `favoriteCount` | uint | 收藏次数 |
| `isPublic` | bool | 是否公开 |
| `isDeleted` | bool | 是否删除 |

---

### TacticsUserModel

用户表（`tactics_user`）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 主键 |
| `oauthProvider` | string | OAuth提供商 |
| `oauthId` | string | OAuth用户ID |
| `levelCode` | string | 用户等级 |
| `nickname` | string? | 昵称 |
| `avatarUrl` | string? | 头像URL |
| `bio` | string? | 个人简介 |
| `isActive` | bool | 是否激活 |
| `uploadCount` | uint | 已上传文件数 |
| `totalDownloadCount` | uint | 总下载次数 |
| `totalLikeCount` | uint | 总点赞数 |

---

### TacticsLikeModel

点赞记录表（`tactics_like`）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 主键 |
| `userId` | long | 用户ID |
| `fileId` | long | 文件ID |
| `createdAt` | DateTime | 点赞时间 |

**唯一约束：** `(userId, fileId)`

---

### TacticsFavoriteModel

收藏记录表（`tactics_favorite`）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 主键 |
| `userId` | long | 用户ID |
| `fileId` | long | 文件ID |
| `createdAt` | DateTime | 收藏时间 |

**唯一约束：** `(userId, fileId)`

---

### TacticsCommentModel

评论表（`tactics_comment`）。

| 字段 | 类型 | 说明 |
|------|------|------|
| `id` | long | 主键 |
| `userId` | long | 评论用户ID |
| `fileId` | long | 文件ID |
| `parentCommentId` | long? | 父评论ID |
| `content` | string | 评论内容（最长1000） |
| `isDeleted` | bool | 是否删除（软删除） |
| `createdAt` | DateTime | 创建时间 |
| `updatedAt` | DateTime | 更新时间 |

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览
- [Constants.md](Constants.md) - 常量定义