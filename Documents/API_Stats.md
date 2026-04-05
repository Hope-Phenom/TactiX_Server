# 统计模块 API

**路由前缀：** `/api/Stats`

> **注意：** 本模块字段命名使用 snake_case（如 `error_Code`），与其他模块的 camelCase 不同。

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/PostExceptionReport` | POST | 无 | 提交异常报告 |
| `/PostVersionControl` | POST | 无 | 版本控制检查 |

---

## 1. 提交异常报告

### 基本信息

```http
POST /api/Stats/PostExceptionReport
```

**认证要求：** 无

**用途：** 客户端提交异常/崩溃报告，用于问题追踪。

### 请求体

```json
{
  "error_Code": "int",
  "error_Desc": "string",
  "feedback_Way": "string?",
  "feedback_Info": "string?",
  "create_Time": "DateTime",
  "is_Read": "bool"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `error_Code` | int | 是 | 错误代码 |
| `error_Desc` | string | 是 | 错误描述 |
| `feedback_Way` | string? | 否 | 反馈方式（如QQ、邮箱等） |
| `feedback_Info` | string? | 否 | 反馈联系信息 |
| `create_Time` | DateTime | 是 | 发生时间 |
| `is_Read` | bool | 是 | 是否已读（默认false） |

### 请求示例

```http
POST /api/Stats/PostExceptionReport
Content-Type: application/json

{
  "error_Code": 5001,
  "error_Desc": "NullReferenceException in TacticsParser.Parse()",
  "feedback_Way": "QQ",
  "feedback_Info": "123456789",
  "create_Time": "2024-03-15T14:30:00",
  "is_Read": false
}
```

### 响应

```
"Save ExceptionReport successfully."
```

---

## 2. 版本控制检查

### 基本信息

```
POST /api/Stats/PostVersionControl
```

**认证要求：** 无

**用途：** 客户端检查是否需要更新版本。

### 请求体

```json
{
  "version": "string"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `version` | string | 是 | 客户端当前版本号 |

### 请求示例

```http
POST /api/Stats/PostVersionControl
Content-Type: application/json

{
  "version": "1.2.3"
}
```

### 响应结构

```json
{
  "lastestVersion": "string",
  "banned": "bool",
  "forceUpgrade": "bool",
  "releaseUrl": "string"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `lastestVersion` | string | 最新版本号 |
| `banned` | bool | 当前版本是否被封禁 |
| `forceUpgrade` | bool | 是否需要强制更新 |
| `releaseUrl` | string | 最新版本下载地址 |

### 响应示例

**需要更新：**
```json
{
  "lastestVersion": "1.3.0",
  "banned": false,
  "forceUpgrade": true,
  "releaseUrl": "https://github.com/xxx/releases/v1.3.0"
}
```

**版本最新：**
```json
{
  "lastestVersion": "1.3.0",
  "banned": false,
  "forceUpgrade": false,
  "releaseUrl": "https://github.com/xxx/releases/latest"
}
```

**版本被封禁：**
```json
{
  "lastestVersion": "1.3.0",
  "banned": true,
  "forceUpgrade": true,
  "releaseUrl": "https://github.com/xxx/releases/v1.3.0"
}
```

### 版本比较规则

使用 `Tools.CompareVersion(string v1, string v2)` 方法：

- 返回 `-1`：v1 < v2（需要更新）
- 返回 `0`：v1 = v2（版本相同）
- 返回 `1`：v1 > v2（版本更高）

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定