# 举报模块 API

**路由前缀：** `/api/Report`

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/` | POST | 需认证 | 提交举报 |
| `/MyReports` | GET | 需认证 | 用户举报历史 |
| `/Pending` | GET | 需认证+管理员 | 待处理举报列表 |
| `/{id}` | GET | 需认证+管理员 | 举报详情 |
| `/{id}/Process` | POST | 需认证+管理员 | 处理举报 |
| `/Stats` | GET | 需认证+管理员 | 举报统计 |

---

## 1. 提交举报

### 基本信息

```http
POST /api/Report
```

**认证要求：** 需要JWT Token

**用途：** 用户举报违规战术文件。

### 请求体

```json
{
  "shareCode": "string",
  "reason": "string",
  "description": "string?"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `shareCode` | string | 是 | 配装码（8字符） |
| `reason` | string | 是 | 举报原因类型 |
| `description` | string | 否 | 详细描述（最多500字符） |

### 举报原因类型

| 代码 | 显示名称 |
|------|----------|
| `inappropriate` | 不当内容 |
| `copyright` | 版权侵权 |
| `malicious` | 恶意代码 |
| `spam` | 垃圾内容 |
| `other` | 其他原因 |

### 请求示例

```http
POST /api/Report
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "shareCode": "00000001",
  "reason": "inappropriate",
  "description": "文件包含违规内容"
}
```

### 响应结构

```json
{
  "reportId": "long",
  "message": "string"
}
```

### 响应示例

```json
{
  "reportId": 1,
  "message": "举报提交成功，我们会尽快处理"
}
```

### 错误响应

| 场景 | 响应 |
|------|------|
| 无效配装码 | `400 { "error": "无效的配装码" }` |
| 文件不存在 | `400 { "error": "文件不存在" }` |
| 已举报过 | `400 { "error": "您已举报过该文件" }` |
| 无效原因 | `400 { "error": "无效的举报原因: xxx" }` |

---

## 2. 用户举报历史

### 基本信息

```http
GET /api/Report/MyReports
```

**认证要求：** 需要JWT Token

**用途：** 获取当前用户的举报历史记录。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 请求示例

```http
GET /api/Report/MyReports?page=1&pageSize=10
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 响应结构

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "reports": [
    {
      "id": "long",
      "shareCode": "string",
      "fileName": "string?",
      "reasonDisplay": "string",
      "statusDisplay": "string",
      "createdAt": "DateTime",
      "handleResult": "string?"
    }
  ]
}
```

---

## 3. 待处理举报列表（管理员）

### 基本信息

```http
GET /api/Report/Pending
```

**认证要求：** 需要JWT Token + 管理员权限

**用途：** 管理员获取待处理的举报列表。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `page` | Query | int | 否 | 页码，默认1 |
| `pageSize` | Query | int | 否 | 每页数量，默认20 |

### 响应结构

```json
{
  "totalCount": "int",
  "page": "int",
  "pageSize": "int",
  "reports": [
    {
      "id": "long",
      "shareCode": "string",
      "fileName": "string?",
      "reason": "string",
      "reasonDisplay": "string",
      "description": "string?",
      "status": "string",
      "statusDisplay": "string",
      "reporterNickname": "string?",
      "createdAt": "DateTime",
      "processedAt": "DateTime?"
    }
  ]
}
```

---

## 4. 举报详情（管理员）

### 基本信息

```http
GET /api/Report/{id}
```

**认证要求：** 需要JWT Token + 管理员权限

**用途：** 管理员查看举报详情。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `id` | Path | long | 是 | 举报ID |

### 响应结构

```json
{
  "id": "long",
  "file": {
    "shareCode": "string",
    "name": "string?",
    "author": "string?",
    "race": "string?",
    "uploaderNickname": "string?"
  },
  "reason": "string",
  "reasonDisplay": "string",
  "description": "string?",
  "status": "string",
  "statusDisplay": "string",
  "reporter": {
    "id": "long",
    "nickname": "string?",
    "avatarUrl": "string?",
    "levelCode": "string?"
  },
  "handler": {
    "id": "long",
    "nickname": "string?",
    "avatarUrl": "string?"
  },
  "handleResult": "string?",
  "createdAt": "DateTime",
  "updatedAt": "DateTime"
}
```

---

## 5. 处理举报（管理员）

### 基本信息

```http
POST /api/Report/{id}/Process
```

**认证要求：** 需要JWT Token + 管理员权限

**用途：** 管理员处理举报（删除文件或忽略）。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `id` | Path | long | 是 | 举报ID |

### 请求体

```json
{
  "takeAction": "bool",
  "handleResult": "string?"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `takeAction` | bool | 是 | true=删除文件，false=忽略举报 |
| `handleResult` | string | 否 | 处理结果说明 |

### 请求示例

```http
POST /api/Report/1/Process
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "takeAction": true,
  "handleResult": "文件已删除，内容违规"
}
```

### 响应示例

**采取行动：**
```json
{
  "message": "举报已处理，相关文件已删除"
}
```

**忽略举报：**
```json
{
  "message": "举报已忽略"
}
```

---

## 6. 举报统计（管理员）

### 基本信息

```http
GET /api/Report/Stats
```

**认证要求：** 需要JWT Token + 管理员权限

**用途：** 获取举报统计数据。

### 响应结构

```json
{
  "pendingCount": "int",
  "processedCount": "int",
  "ignoredCount": "int",
  "totalCount": "int"
}
```

---

## 数据流说明

```
用户举报流程:
1. 用户发现违规内容 → 提交举报
2. 系统创建举报记录（status=pending）
3. 管理员查看待处理列表
4. 管理员处理举报:
   - takeAction=true → 删除文件、更新状态为processed
   - takeAction=false → 更新状态为ignored
5. （可选）发送通知给举报者
```

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定
- [Constants.md](Constants.md) - 举报原因和状态定义