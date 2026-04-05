# 认证模块 API

**路由前缀：** `/api/Auth`

---

## 接口列表

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/Login/{provider}` | GET | 无 | 获取OAuth登录URL |
| `/Callback/{provider}` | GET | 无 | OAuth回调处理 |
| `/DevLogin` | GET | 无 | 开发模式登录 |
| `/LevelInfo` | GET | 需认证 | 获取用户等级信息 |

---

## 1. 获取OAuth登录URL

### 基本信息

```http
GET /api/Auth/Login/{provider}
```

**认证要求：** 无

**用途：** 获取第三方OAuth登录的起始URL，客户端引导用户跳转至此URL完成授权。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `provider` | Path | string | 是 | OAuth提供商：`qq`, `wechat`, `dev` |
| `redirectUri` | Query | string | 否 | 授权后重定向URI，默认`/` |

### 请求示例

```http
GET /api/Auth/Login/qq?redirectUri=/tactics
```

### 响应结构

```json
{
  "provider": "string",
  "loginUrl": "string",
  "state": "string"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `provider` | string | OAuth提供商名称 |
| `loginUrl` | string | 用户应跳转的授权URL |
| `state` | string | 状态令牌（GUID格式，用于防CSRF） |

### 响应示例

```json
{
  "provider": "qq",
  "loginUrl": "https://graph.qq.com/oauth2.0/authorize?response_type=code&client_id=xxx&redirect_uri=xxx&state=a1b2c3d4e5f6",
  "state": "a1b2c3d4e5f6"
}
```

### 错误响应

```json
{
  "message": "不支持的登录提供商: google"
}
```

---

## 2. OAuth回调处理

### 基本信息

```http
GET /api/Auth/Callback/{provider}
```

**认证要求：** 无

**用途：** OAuth提供商授权完成后回调此接口，完成用户注册/登录并返回Token。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `provider` | Path | string | 是 | OAuth提供商名称 |
| `code` | Query | string | 是 | OAuth授权码 |
| `state` | Query | string | 否 | 状态令牌（用于验证） |

### 请求示例

```http
GET /api/Auth/Callback/qq?code=ABC123XYZ&state=a1b2c3d4e5f6
```

### 响应结构

```json
{
  "message": "string",
  "userId": "long",
  "nickname": "string",
  "avatarUrl": "string?",
  "levelCode": "string",
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": "int"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `message` | string | 结果消息 |
| `userId` | long | 用户唯一ID |
| `nickname` | string | 用户昵称 |
| `avatarUrl` | string? | 用户头像URL（可空） |
| `levelCode` | string | 用户等级代码（`normal/verified/pro/admin`） |
| `accessToken` | string | JWT访问令牌 |
| `refreshToken` | string | 刷新令牌 |
| `expiresIn` | int | Token有效期（秒） |

### 响应示例

```json
{
  "message": "登录成功",
  "userId": 12345,
  "nickname": "SC2Player",
  "avatarUrl": "https://example.com/avatar.jpg",
  "levelCode": "normal",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "rt_abc123xyz",
  "expiresIn": 7200
}
```

### 数据流说明

```
1. OAuth回调 → 验证授权码
2. 获取OAuth用户信息 → 查询本地用户
   ┌─────────────────────────────────────┐
   │ 用户已存在 → 更新昵称/头像 → 生成Token │
   │ 用户不存在 → 创建新用户 → 生成Token   │
   │ 新用户昵称匹配超管 → 自动赋予admin权限 │
   └─────────────────────────────────────┘
3. 返回Token和用户信息
```

### 错误响应

| 场景 | 响应 |
|------|------|
| 不支持的提供商 | `{ "message": "不支持的登录提供商: xxx" }` |
| 授权失败 | `{ "message": "登录失败" }` |
| 内部错误 | `500 { "message": "登录处理失败，请稍后重试" }` |

---

## 3. 开发模式登录

### 基本信息

```http
GET /api/Auth/DevLogin
```

**认证要求：** 无

**用途：** 仅用于开发测试，无需OAuth流程直接获取Token。

**注意：** 生产环境应禁用此接口。

### 请求参数

| 参数 | 位置 | 类型 | 必需 | 说明 |
|------|------|------|------|------|
| `devUserId` | Query | string | 否 | 开发用户标识，默认`test` |
| `state` | Query | string | 否 | 状态令牌 |

### 请求示例

```http
GET /api/Auth/DevLogin?devUserId=test_user
```

### 响应结构

同 `/Callback/{provider}` 响应结构。

### 响应示例

```json
{
  "message": "登录成功",
  "userId": 1,
  "nickname": "test_user",
  "avatarUrl": null,
  "levelCode": "normal",
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "rt_xxx",
  "expiresIn": 7200
}
```

### 使用场景

- 本地开发测试
- 自动化集成测试
- 无需真实OAuth环境快速验证API

---

## 4. 获取用户等级信息

### 基本信息

```http
GET /api/Auth/LevelInfo
```

**认证要求：** 需要JWT Token

**用途：** 获取当前登录用户的等级配置信息（文件上传限制等）。

### 请求参数

无额外参数，通过Token识别用户。

### 请求示例

```http
GET /api/Auth/LevelInfo
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

### 响应结构

```json
{
  "levelCode": "string",
  "levelName": "string",
  "description": "string?",
  "badgeColor": "string?",
  "maxFileSize": "uint",
  "maxUploadCount": "uint",
  "dailyUploadLimit": "uint",
  "instantNotification": "bool"
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `levelCode` | string | 等级代码 |
| `levelName` | string | 等级名称（如"普通用户"） |
| `description` | string? | 等级描述 |
| `badgeColor` | string? | 徽章颜色（如"#95a5a6"） |
| `maxFileSize` | uint | 单文件最大大小（字节） |
| `maxUploadCount` | uint | 允许上传的文件总数 |
| `dailyUploadLimit` | uint | 每日上传限制 |
| `instantNotification` | bool | 是否即时通知审核结果 |

### 响应示例

```json
{
  "levelCode": "normal",
  "levelName": "普通用户",
  "description": "普通用户，基础权限",
  "badgeColor": "#95a5a6",
  "maxFileSize": 10485760,
  "maxUploadCount": 10,
  "dailyUploadLimit": 3,
  "instantNotification": false
}
```

### 错误响应

| 状态码 | 场景 | 响应 |
|--------|------|------|
| 401 | 未登录 | `{ "message": "请先登录" }` |
| 404 | 用户不存在 | `{ "message": "用户不存在" }` |

---

## QQ OAuth 登录

### 环境配置

QQ OAuth需要配置以下环境变量：

| 变量名 | 说明 | 示例值 |
|--------|------|--------|
| `TACTIX_QQ_APP_ID` | QQ互联App ID | `你的AppId` |
| `TACTIX_QQ_APP_KEY` | QQ互联App Key | `你的AppKey` |
| `TACTIX_QQ_CALLBACK_URL` | 回调地址 | `https://你的域名/api/Auth/Callback/qq` |

**注意：** 开发环境（DEBUG模式）不加载QQ服务，使用DevLogin进行测试。

### QQ OAuth流程

```
1. GET /api/Auth/Login/qq → 返回QQ授权URL
2. 用户扫码授权 → QQ回调到 CallbackUrl
3. GET /api/Auth/Callback/qq?code=xxx → 处理登录
   ├── 用code换取AccessToken（GET请求，URL编码响应）
   ├── 用AccessToken获取OpenID（JSONP格式响应）
   ├── 用AccessToken+OpenID获取用户信息
   └── 创建/更新用户，返回JWT Token
```

### QQ API特殊处理

QQ互联的OAuth实现与标准OAuth 2.0有差异：

| API | 标准OAuth | QQ互联 | 服务端处理 |
|-----|----------|--------|----------|
| Token交换 | POST请求，JSON响应 | GET请求，URL编码响应 | ParseQueryString解析 |
| OpenID获取 | 标准JSON响应 | JSONP格式`callback({...})` | 去除callback包装 |
| 用户信息参数 | client_id | oauth_consumer_key | 使用QQ特有参数名 |

### 获取的用户信息

QQ OAuth登录获取以下用户信息：

| 字段 | QQ API字段 | 说明 |
|------|----------|------|
| OAuthId | openid | 用户唯一标识（QQ OpenID） |
| Nickname | nickname | 用户QQ昵称 |
| AvatarUrl | figureurl_2 | 用户头像URL（100x100） |

---

## 测试端点（仅DEBUG模式）

以下端点仅在开发环境（DEBUG编译）可用：

| 端点 | 方法 | 说明 |
|------|------|------|
| `/TestShareCode` | GET | 测试配装码编解码 |
| `/TestParser` | POST | 测试文件解析器 |
| `/TestHash` | GET | 测试哈希工具 |
| `/TestValidator` | POST | 测试文件验证器 |
| `/TestXssBlock` | POST | 测试XSS拦截 |

### TestShareCode 示例

```http
GET /api/Auth/TestShareCode?id=1&code=00000000
```

响应包含编解码验证结果和批量测试用例。

---

## 完整认证流程图

```
┌──────────────────────────────────────────────────────────────┐
│                     客户端                                    │
└──────────────────────────────────────────────────────────────┘
           │
           │ 1. GET /api/Auth/Login/{provider}
           ▼
┌──────────────────────────────────────────────────────────────┐
│                     TactiX Server                            │
│  返回: { loginUrl, state }                                   │
└──────────────────────────────────────────────────────────────┘
           │
           │ 2. 跳转 loginUrl
           ▼
┌──────────────────────────────────────────────────────────────┐
│                     OAuth Provider                           │
│  (QQ/WeChat)                                                 │
│  用户授权 → 返回 code                                         │
└──────────────────────────────────────────────────────────────┘
           │
           │ 3. 重定向到 /api/Auth/Callback/{provider}?code=xxx
           ▼
┌──────────────────────────────────────────────────────────────┐
│                     TactiX Server                            │
│  验证code → 获取用户信息 → 创建/更新用户 → 生成JWT             │
│  返回: { accessToken, userId, ... }                          │
└──────────────────────────────────────────────────────────────┘
           │
           │ 4. 存储 accessToken，后续请求携带
           ▼
┌──────────────────────────────────────────────────────────────┐
│                     客户端                                    │
│  Authorization: Bearer {token}                               │
└──────────────────────────────────────────────────────────────┘
```

---

## 相关文档

- [API_Overview.md](API_Overview.md) - API总览与通用约定
- [Constants.md](Constants.md) - 用户等级权限矩阵