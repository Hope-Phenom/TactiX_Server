# TactiX 战术大厅功能设计方案

> 本文档是战术大厅功能的完整设计方案和实施指南
> 创建时间: 2025-03-29
> 当前阶段: Phase 1 已完成

---

## 项目概述

为TactiX应用添加"战术大厅"功能，允许用户上传、分享、检索.tactix战术文件。战术文件由用户手动创建或通过解析星际争霸II游戏回放文件生成。

---

## 设计决策

| 模块 | 决策 |
|------|------|
| **用户系统** | OAuth第三方登录（QQ + 微信）+ JWT API认证 |
| **用户分级** | 普通用户 / 认证作者 / 职业选手 / 管理员（四级） |
| **权限控制** | 数据库配置表，支持每级别差异化配置 |
| **文件存储** | 混合方案 - 元数据存MySQL，实际文件存磁盘 |
| **配装码** | 8位字母数字编码（62进制，如X7K9M2P3） |
| **审核机制** | 先审后发 - 所有用户上传需审核，认证作者/职业选手即时通知，普通用户批量汇总通知 |
| **检索维度** | 种族、对抗类型、战术风格、关键词搜索 |
| **版本管理** | 版本链 - 同一配装码支持多版本，可查看历史 |
| **匿名访问** | 允许匿名下载，上传/点赞必须登录 |
| **通知系统** | 邮件 + 飞书WebHook，即时通知+定时汇总 |
| **文件安全** | 内容验证 + 格式检查 + 防恶意代码注入 |

---

## tactix文件格式

```json
{
  "Id": "guid",
  "Name": "战术名称",
  "Author": "作者",
  "Description": "描述",
  "ApplicableVersion": "1",
  "TacticType": 0,
  "TacVersion": 1,
  "UpdateTime": "2000-01-01|00:00",
  "ModName": "StarCraft2",
  "ModVersion": 1,
  "Actions": [...]
}
```

**种族识别**：从Actions中的ItemAbbr前缀判断
- P开头 = Protoss神族
- T开头 = Terran人族
- Z开头 = Zerg虫族

---

## 数据库表结构

### 1. user_level_config - 用户等级配置表
### 2. tactics_user - 用户表
### 3. tactics_admin - 管理员表
### 4. tactics_file - 战术文件主表
### 5. tactics_file_version - 文件版本表
### 6. tactics_like - 点赞表
### 7. tactics_audit_log - 审核记录表
### 8. notification_config - 通知配置表
### 9. notification_log - 通知日志表

详见 `SQLScript/tactics_hall_tables.sql`

---

## 用户等级与权限

| 等级 | 上传总数 | 单文件大小 | 版本数 | 日上传 | 审核通知方式 |
|------|---------|-----------|--------|--------|-------------|
| normal | 10 | 10MB | 5 | 3 | **批量汇总**（60分钟） |
| verified | 50 | 20MB | 10 | 10 | **即时通知** |
| pro | 200 | 50MB | 20 | 50 | **即时通知** |
| admin | 1000 | 100MB | 50 | 100 | **即时通知** |

**注意**: 所有用户上传的文件都需要审核后才能公开

### 等级升级流程
```
normal → verified [需申请+审核]
      → pro [需申请+审核]
```

---

## 文件安全验证（5层）

```
1. 基础验证
   - 扩展名 .tactix
   - MIME类型检查
   - 大小限制

2. 格式验证
   - JSON合法性
   - 必需字段检查
   - 字段类型验证

3. 内容深度验证
   - Actions结构
   - ItemAbbr格式
   - Supply格式 "13/15"
   - Time值合理性

4. 安全验证
   - 字符串长度限制
   - XSS特殊字符过滤
   - Base64/Unicode隐藏代码检测
   - 文件签名检查

5. 哈希校验
   - SHA256计算
   - 重复文件检查
```

---

## 通知系统

### 即时通知
- storage_low (存储空间低于85%)
- storage_critical (存储空间低于95%)
- error_exception (系统运行异常)
- upload_fail (文件上传失败)
- security_alert (安全告警)

### 定时汇总
- **pending_audit_batch**: 60分钟汇总普通用户上传
- **daily_stats**: 24小时日活统计

---

## 文件存储结构

```
/uploads/tactics/
├── 2025/
│   ├── 03/
│   │   ├── 29/
│   │   │   ├── X7K9M2P3_v1.tactix
│   │   │   └── X7K9M2P3_v2.tactix
├── temp/              # 临时上传目录
└── quarantine/        # 隔离区（可疑文件）
```

---

## 配装码算法

使用62进制（0-9a-zA-Z）将数据库ID转换为8位编码：

```csharp
const string BASE62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

public static string ToShareCode(long id)
{
    var result = new char[8];
    for (int i = 7; i >= 0; i--)
    {
        result[i] = BASE62[(int)(id % 62)];
        id /= 62;
    }
    return new string(result);
}
```

---

## 实施阶段

### Phase 1: 基础架构与认证 ✅ 已完成
- [x] 数据库表创建
- [x] 数据模型（Entity Framework）
- [x] TacticsDbContext
- [x] JWT服务
- [x] OAuth基础框架
- [x] 开发模式登录
- [x] AuthController
- [x] 基础配置类

### Phase 2: 文件上传与安全
- [ ] 文件上传API
- [ ] 文件安全验证器
- [ ] tactix文件解析
- [ ] 配装码生成
- [ ] 权限检查

### Phase 3: 版本管理与检索
- [ ] 版本链功能
- [ ] 多维度检索API
- [ ] 全文搜索
- [ ] 下载计数

### Phase 4: 审核机制与通知
- [ ] 审核状态管理
- [ ] 审核队列分级
- [ ] 即时/批量通知

### Phase 5: 通知系统
- [ ] 邮件服务
- [ ] 飞书WebHook
- [ ] 通知后台服务

### Phase 6: 社交功能
- [ ] 点赞
- [ ] 个人中心
- [ ] 排行榜

---

## 关键文件路径

### 已完成（Phase 1）
```
TactiX_Server/
├── Models/Tactics/
│   ├── UserLevelConfigModel.cs
│   ├── TacticsUserModel.cs
│   ├── TacticsAdminModel.cs
│   ├── TacticsFileModel.cs
│   ├── TacticsFileVersionModel.cs
│   ├── TacticsLikeModel.cs
│   ├── TacticsAuditLogModel.cs
│   ├── NotificationConfigModel.cs
│   └── NotificationLogModel.cs
├── Models/Config/
│   ├── JwtConfig.cs
│   └── TacticsHallConfig.cs
├── Data/
│   └── TacticsDbContext.cs
├── Services/
│   ├── JwtService.cs
│   ├── AdminService.cs
│   ├── IOAuthProvider.cs
│   └── DevAuthService.cs
├── Controllers/
│   └── AuthController.cs
├── Middleware/
│   └── JwtAuthMiddleware.cs
└── Program.cs (已更新)
```

### 待创建（Phase 2+）
```
Services/
├── PermissionService.cs
├── TacticsFileService.cs
├── ShareCodeService.cs
├── FileSecurityValidator.cs
├── NotificationService.cs
├── EmailService.cs
├── FeishuService.cs
└── NotificationBackgroundService.cs

Utils/
├── TacticsFileParser.cs
├── ShareCodeUtil.cs
└── HashUtil.cs

Controllers/
├── UserController.cs
├── TacticsHallController.cs
└── AdminController.cs
```

---

## 配置说明

### appsettings.json

```json
{
  "TacticsHall": {
    "StoragePath": "/app/uploads/tactics",
    "TempPath": "/app/uploads/temp",
    "QuarantinePath": "/app/uploads/quarantine",
    "MaxFileSize": 10485760,
    "AllowedExtensions": [".tactix"],
    "Security": {
      "MaxStringLength": 10000,
      "MaxActionsCount": 1000,
      "EnableXssFilter": true,
      "EnableDuplicateCheck": true
    }
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-characters",
    "Issuer": "TactiX",
    "Audience": "TactiX-Client",
    "AccessTokenExpiryHours": 2,
    "RefreshTokenExpiryDays": 7
  }
}
```

### 环境变量

```bash
# 现有
TACTIX_FORUM_USERNAME
TACTIX_FORUM_PASSWORD
TACTIX_CONNCTION_STRINGS

# 新增（Phase 5通知系统）
TACTIX_SMTP_SERVER
TACTIX_SMTP_PORT
TACTIX_SMTP_USERNAME
TACTIX_SMTP_PASSWORD
TACTIX_FEISHU_WEBHOOK_URL
TACTIX_FEISHU_SECRET
```

---

## API端点

### 认证模块 (/api/Auth)
- `GET /api/Auth/Login/{provider}` - 获取OAuth登录URL
- `GET /api/Auth/Callback/{provider}` - OAuth回调处理
- `GET /api/Auth/DevLogin` - 开发模式登录（测试用）
- `POST /api/Auth/RefreshToken` - 刷新JWT Token
- `POST /api/Auth/Logout` - 登出
- `GET /api/Auth/LevelInfo` - 获取用户等级信息

### 战术大厅模块 (/api/TacticsHall) - Phase 2+
- `POST /api/TacticsHall/Upload` - 上传战术文件
- `GET /api/TacticsHall/Download/{shareCode}` - 下载
- `GET /api/TacticsHall/Detail/{shareCode}` - 详情
- `GET /api/TacticsHall/Search` - 搜索

### 管理员模块 (/api/Admin) - Phase 4+
- `GET /api/Admin/PendingFiles` - 待审核列表
- `POST /api/Admin/Approve/{fileId}` - 通过审核
- `POST /api/Admin/Reject/{fileId}` - 拒绝审核

---

## 注意事项

1. **超级管理员**: `hope_phenom` 作为内置超级管理员
2. **QQ/微信OAuth**: 需到开放平台申请AppID和AppSecret
3. **文件存储**: 确保目录权限正确（读写执行）
4. **数据库**: 使用utf8mb4字符集支持emoji
5. **JWT密钥**: 生产环境必须修改默认密钥，至少32位

---

## 继续开发指南

下回开发时：
1. 确认在 `features/tactics_hall` 分支
2. Phase 2 开始开发文件上传API
3. 先创建 `FileSecurityValidator.cs` 进行文件安全验证
4. 参考本设计方案继续实现

---

## 相关文档

- `Documents/TacticsHall_Development.md` - 开发日志和详细说明
- `SQLScript/tactics_hall_tables.sql` - 数据库脚本
- `/home/hope_phenom/.claude/plans/sleepy-mixing-ripple.md` - 完整计划文件
