# 战术大厅核心模块 API

**路由前缀：** `/api/TacticsHall`

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/Upload` | POST | 需认证 | 上传战术文件 |
| `/UploadVersion` | POST | 需认证 | 上传文件新版本 |
| `/Detail/{shareCode}` | GET | 无 | 获取文件详情 |
| `/Search` | GET | 无 | 搜索战术文件 |
| `/Download/{shareCode}` | GET | 无 | 下载战术文件 |
| `/Versions/{shareCode}` | GET | 无 | 获取版本列表 |
| `/Delete/{shareCode}` | DELETE | 需认证 | 删除文件 |
| `/PendingReview` | GET | 需认证+管理员 | 待审核列表 |
| `/Review/{shareCode}` | POST | 需认证+管理员 | 审核文件 |
| `/BatchReview` | POST | 需认证+管理员 | 批量审核 |

---

## 1. 上传战术文件

### 基本信息

```
POST /api/TacticsHall/Upload
```

**认证要求：** 需要JWT Token

**请求大小限制：** 100MB

**用途：** 上传新的战术文件，文件进入待审核状态。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `file` | Form | IFormFile | 是 | `.tactix`格式文件 |
| `Changelog` | Form | string | 否 | 版本更新说明，最长1000字符 |

### 请求示例

```http
POST /api/TacticsHall/Upload
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="file"; filename="tactics.tactix"
Content-Type: application/octet-stream

{文件二进制内容}
--boundary
Content-Disposition: form-data; name="Changelog"

初始版本
--boundary--
```

### 响应结构

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
| `shareCode` | string | 8字符配装码（用于分享） |
| `fileId` | long | 文件唯一ID |
| `versionNumber` | int | 版本号（初始为1） |
| `status` | string | 状态：`pending/approved/rejected` |
| `message` | string | 提示消息 |

### 响应示例

```json
{
  "shareCode": "000000AB",
  "fileId": 123,
  "versionNumber": 1,
  "status": "pending",
  "message": "上传成功，文件正在审核中"
}
```

### 数据流说明

```
1. 接收文件 → 验证格式(.tactix)
2. FileSecurityValidator 5层验证：
   ├─ 结构验证（JSON格式）
   ├─ 大小验证（不超过用户等级限制）
   ├─ 内容验证（必需字段）
   ├─ XSS验证（无恶意脚本）
   └─ 哈希验证（计算SHA256）
3. TacticsFileParser 解析：
   ├─ 提取 name, author
   └─ 从 actions 识别种族(P/T/Z)
4. PermissionService 权限检查：
   ├─ CanUploadAsync（是否有上传权限）
   ├─ 每日上传限制检查
   └─ 文件总数限制检查
5. 保存文件 → 生成ShareCode → 写入数据库
6. 返回配装码和待审核状态
```

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 400 | 未上传文件 | `{ "error": "请上传文件" }` |
| 400 | 格式错误 | `{ "error": "只支持.tactix格式文件" }` |
| 401 | 未登录 | `{ "error": "未登录或登录已过期" }` |
| 400 | 权限不足 | `{ "error": "无上传权限" }` |
| 400 | 超出限制 | `{ "error": "已达到每日上传限制" }` |

---

## 2. 上传文件新版本

### 基本信息

```
POST /api/TacticsHall/UploadVersion
```

**认证要求：** 需要JWT Token

**请求大小限制：** 100MB

**用途：** 为已存在的文件上传新版本，仅文件上传者或管理员可操作。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `file` | Form | IFormFile | 是 | `.tactix`格式文件 |
| `ShareCode` | Form | string | 是 | 8字符配装码 |
| `Changelog` | Form | string | 否 | 版本更新说明 |

### 请求示例

```http
POST /api/TacticsHall/UploadVersion
Authorization: Bearer {token}
Content-Type: multipart/form-data

--boundary
Content-Disposition: form-data; name="file"; filename="v2.tactix"

{文件内容}
--boundary
Content-Disposition: form-data; name="ShareCode"

000000AB
--boundary
Content-Disposition: form-data; name="Changelog"

修复了开局时间点
--boundary--
```

### 响应结构

同 `/Upload` 响应结构，`versionNumber` 为新版本号。

### 响应示例

```json
{
  "shareCode": "000000AB",
  "fileId": 123,
  "versionNumber": 2,
  "status": "pending",
  "message": "新版本上传成功，文件正在审核中"
}
```

### 权限说明

- **文件上传者**：可上传自己文件的新版本
- **管理员**：可上传任何文件的新版本
- **版本限制**：不超过用户等级的 `MaxVersionPerFile`

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 400 | 无效配装码 | `{ "error": "无效的配装码格式" }` |
| 404 | 文件不存在 | `{ "error": "战术文件不存在" }` |
| 403 | 无权限 | `{ "error": "只有上传者和管理员可以上传新版本" }` |
| 400 | 超出版本限制 | `{ "error": "已达到最大版本数限制" }` |

---

## 3. 获取文件详情

### 基本信息

```
GET /api/TacticsHall/Detail/{shareCode}
```

**认证要求：** 无（可选携带Token获取互动状态）

**用途：** 获取战术文件的完整详情信息。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |

### 请求示例

```http
GET /api/TacticsHall/Detail/000000AB
Authorization: Bearer {token}  # 可选，用于获取互动状态
```

### 响应结构

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
  "uploader": {
    "id": "long",
    "nickname": "string?",
    "avatarUrl": "string?",
    "levelCode": "string?",
    "levelName": "string?"
  }
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `shareCode` | string | 配装码 |
| `name` | string? | 战术名称（来自文件） |
| `author` | string? | 作者名称（来自文件） |
| `race` | string? | 种族代码（P/T/Z） |
| `raceDisplay` | string? | 种族显示名（神族/人族/虫族） |
| `fileSize` | long | 文件大小（字节） |
| `downloadCount` | uint | 下载次数 |
| `likeCount` | uint | 点赞数 |
| `favoriteCount` | uint | 收藏数 |
| `isLikedByUser` | bool | 当前用户是否已点赞（需Token） |
| `isFavoritedByUser` | bool | 当前用户是否已收藏（需Token） |
| `currentVersion` | int | 当前版本号 |
| `status` | string | 审核状态 |
| `createdAt` | DateTime | 上传时间 |
| `updatedAt` | DateTime | 更新时间 |
| `uploader` | object | 上传者信息 |

### 响应示例

```json
{
  "shareCode": "000000AB",
  "name": "PvZ开局战术",
  "author": "ProtossMaster",
  "race": "P",
  "raceDisplay": "神族",
  "fileSize": 2048,
  "downloadCount": 150,
  "likeCount": 42,
  "favoriteCount": 15,
  "isLikedByUser": false,
  "isFavoritedByUser": true,
  "currentVersion": 3,
  "status": "approved",
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-02-20T15:45:00Z",
  "uploader": {
    "id": 12345,
    "nickname": "SC2Player",
    "avatarUrl": "https://example.com/avatar.jpg",
    "levelCode": "verified",
    "levelName": "认证作者"
  }
}
```

### 权限说明

- **已审核文件**：所有人可查看
- **未审核文件**：仅上传者和管理员可查看

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 400 | 无效配装码 | `{ "error": "无效的配装码格式" }` |
| 404 | 文件不存在 | `{ "error": "战术文件不存在" }` |
| 404 | 未审核无权限 | `{ "error": "战术文件不存在或未通过审核" }` |

---

## 4. 搜索战术文件

### 基本信息

```
GET /api/TacticsHall/Search
```

**认证要求：** 无

**用途：** 搜索已审核通过的战术文件列表。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `Keyword` | Query | string | 否 | 关键词搜索（名称/作者），最长100字符 |
| `Race` | Query | string | 否 | 种族筛选：`P/T/Z` |
| `UploaderId` | Query | long | 否 | 上传者ID筛选 |
| `SortBy` | Query | string | 否 | 排序方式：`latest/popular/downloads`，默认`latest` |
| `Page` | Query | int | 否 | 页码，默认1 |
| `PageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/TacticsHall/Search?Keyword=开局&Race=P&SortBy=popular&Page=1&PageSize=10
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
      "createdAt": "DateTime",
      "uploaderNickname": "string?"
    }
  ]
}
```

### 响应示例

```json
{
  "totalCount": 25,
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
      "likeCount": 42,
      "currentVersion": 3,
      "createdAt": "2024-01-15T10:30:00Z",
      "uploaderNickname": "SC2Player"
    }
  ]
}
```

---

## 5. 下载战术文件

### 基本信息

```
GET /api/TacticsHall/Download/{shareCode}
```

**认证要求：** 无（未审核文件需要上传者或管理员）

**用途：** 下载战术文件内容。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |
| `version` | Query | int | 否 | 版本号（默认最新版本） |

### 请求示例

```http
GET /api/TacticsHall/Download/000000AB
GET /api/TacticsHall/Download/000000AB?version=2
```

### 响应

- **Content-Type:** `application/octet-stream`
- **文件名:** `{shareCode}.tactix`
- **Body:** 文件二进制流

### 数据流说明

```
1. 解码 shareCode → fileId
2. 检查文件存在性和审核状态
3. 确定下载版本（最新或指定版本）
4. 流式传输文件内容
5. 更新 downloadCount（仅已审核文件）
```

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 400 | 无效配装码 | `{ "error": "无效的配装码格式" }` |
| 404 | 文件不存在 | `{ "error": "文件不存在" }` |
| 404 | 版本不存在 | `{ "error": "版本不存在" }` |

---

## 6. 获取版本列表

### 基本信息

```
GET /api/TacticsHall/Versions/{shareCode}
```

**认证要求：** 无

**用途：** 获取文件的所有版本历史。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |

### 请求示例

```http
GET /api/TacticsHall/Versions/000000AB
```

### 响应结构

```json
{
  "shareCode": "string",
  "versions": [
    {
      "versionNumber": "int",
      "fileSize": "long",
      "changelog": "string?",
      "createdAt": "DateTime"
    }
  ]
}
```

### 响应示例

```json
{
  "shareCode": "000000AB",
  "versions": [
    {
      "versionNumber": 1,
      "fileSize": 2048,
      "changelog": "初始版本",
      "createdAt": "2024-01-15T10:30:00Z"
    },
    {
      "versionNumber": 2,
      "fileSize": 2150,
      "changelog": "修复了开局时间点",
      "createdAt": "2024-02-10T14:20:00Z"
    },
    {
      "versionNumber": 3,
      "fileSize": 2200,
      "changelog": "添加后期战术",
      "createdAt": "2024-02-20T15:45:00Z"
    }
  ]
}
```

---

## 7. 删除战术文件

### 基本信息

```
DELETE /api/TacticsHall/Delete/{shareCode}
```

**认证要求：** 需要JWT Token

**用途：** 软删除战术文件（标记为已删除）。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |

### 请求示例

```http
DELETE /api/TacticsHall/Delete/000000AB
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

- **普通用户**：只能删除自己上传的文件（需 `CanDeleteOwnFile` 权限）
- **管理员**：可删除任何文件

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 401 | 未登录 | `{ "error": "未登录或登录已过期" }` |
| 403 | 无权限 | `{ "error": "只能删除自己的文件" }` |

---

## 8. 待审核文件列表（管理员）

### 基本信息

```
GET /api/TacticsHall/PendingReview
```

**认证要求：** 需要JWT Token + 管理员权限

**用途：** 获取所有待审核状态的文件列表。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/TacticsHall/PendingReview?page=1&pageSize=20
Authorization: Bearer {admin_token}
```

### 响应结构

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "files": [
    {
      "id": "long",
      "shareCode": "string",
      "name": "string?",
      "author": "string?",
      "race": "string?",
      "raceDisplay": "string?",
      "fileSize": "long",
      "createdAt": "DateTime",
      "uploaderId": "long",
      "uploaderNickname": "string?"
    }
  ]
}
```

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 401 | 未登录 | `{ "error": "未登录或登录已过期" }` |
| 403 | 无管理员权限 | `Forbid()` |

---

## 9. 审核文件（管理员）

### 基本信息

```
POST /api/TacticsHall/Review/{shareCode}
```

**认证要求：** 需要JWT Token + 管理员权限

**用途：** 审核单个文件（通过或拒绝）。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `shareCode` | Path | string | 是 | 8字符配装码 |

### 请求体

```json
{
  "approved": "bool"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `approved` | bool | true=通过，false=拒绝 |

### 请求示例

```http
POST /api/TacticsHall/Review/000000AB
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "approved": true
}
```

### 响应结构

```json
{
  "message": "string",
  "shareCode": "string",
  "status": "string"
}
```

### 响应示例

```json
{
  "message": "审核通过",
  "shareCode": "000000AB",
  "status": "approved"
}
```

---

## 10. 批量审核（管理员）

### 基本信息

```
POST /api/TacticsHall/BatchReview
```

**认证要求：** 需要JWT Token + 管理员权限

**用途：** 批量审核多个文件。

### 请求体

```json
{
  "shareCodes": ["string[]"],
  "approved": "bool"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `shareCodes` | string[] | 配装码列表 |
| `approved` | bool | true=全部通过，false=全部拒绝 |

### 请求示例

```http
POST /api/TacticsHall/BatchReview
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "shareCodes": ["000000AB", "000000CD", "000000EF"],
  "approved": true
}
```

### 响应结构

```json
{
  "message": "string",
  "results": [
    {
      "shareCode": "string",
      "status": "string"
    }
  ]
}
```

### 响应示例

```json
{
  "message": "已审核 3 个文件",
  "results": [
    { "shareCode": "000000AB", "status": "approved" },
    { "shareCode": "000000CD", "status": "approved" },
    { "shareCode": "000000EF", "status": "approved" }
  ]
}
```

---

## 文件状态流转图

```
                    ┌─────────────┐
                    │  上传文件    │
                    └─────────────┘
                          │
                          ▼
                    ┌─────────────┐
                    │   pending   │ ←──── 新上传文件初始状态
                    │   (待审核)   │
                    └─────────────┘
                          │
              ┌───────────┴───────────┐
              │                       │
        审核通过                  审核拒绝
              │                       │
              ▼                       ▼
    ┌─────────────┐           ┌─────────────┐
    │  approved   │           │  rejected   │
    │  (已通过)    │           │  (已拒绝)    │
    └─────────────┘           └─────────────┘
          │                         │
          │                    可重新上传版本
          │                    进入pending重新审核
          ▼
    可下载/点赞/收藏/评论
```

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定
- [API_TacticsInteraction.md](API_TacticsInteraction.md) - 战术互动接口
- [FileFormat.md](FileFormat.md) - .tactix文件格式规范