# TactiX 战术大厅开发文档

## 项目概述

战术大厅是TactiX_Server的新功能模块，允许用户上传、分享、检索.tactix战术文件。

## 技术架构

### 技术栈
- **框架**: ASP.NET Core 10.0
- **数据库**: MySQL 8.0 + Entity Framework Core 9.0
- **认证**: OAuth 2.0 (QQ/微信) + JWT
- **文件存储**: 本地文件系统 + 数据库存储元数据
- **通知**: 邮件 + 飞书WebHook

### 目录结构

```
TactiX_Server/
├── Controllers/              # API控制器
│   ├── AuthController.cs     # 认证相关
│   ├── UserController.cs     # 用户相关
│   ├── TacticsHallController.cs  # 战术大厅
│   └── AdminController.cs    # 管理员
├── Data/                     # 数据库上下文
│   ├── NewsDbContext.cs      # 现有-新闻
│   ├── StatsDbContext.cs     # 现有-统计
│   └── TacticsDbContext.cs   # 新增-战术大厅
├── Models/                   # 数据模型
│   ├── Tactics/              # 战术大厅模型
│   │   ├── TacticsUserModel.cs
│   │   ├── TacticsAdminModel.cs
│   │   ├── TacticsFileModel.cs
│   │   ├── TacticsFileVersionModel.cs
│   │   ├── UserLevelConfigModel.cs
│   │   ├── NotificationConfigModel.cs
│   │   └── NotificationLogModel.cs
│   ├── News/                 # 现有-新闻
│   ├── Stats/                # 现有-统计
│   ├── Req/                  # 请求模型
│   └── Resp/                 # 响应模型
├── Services/                 # 业务服务
│   ├── JwtService.cs         # JWT服务
│   ├── AdminService.cs       # 管理员权限
│   ├── PermissionService.cs  # 权限检查
│   ├── IOAuthProvider.cs     # OAuth接口
│   ├── QQAuthService.cs      # QQ登录
│   ├── WechatAuthService.cs  # 微信登录
│   ├── DevAuthService.cs     # 开发模式登录
│   ├── TacticsFileService.cs # 文件服务
│   ├── ShareCodeService.cs   # 配装码服务
│   ├── FileSecurityValidator.cs  # 文件安全验证
│   ├── NotificationService.cs    # 通知服务
│   ├── EmailService.cs       # 邮件服务
│   ├── FeishuService.cs      # 飞书服务
│   └── NewsGenerateService.cs    # 现有-新闻生成
├── Utils/                    # 工具类
│   ├── TacticsFileParser.cs  # tactix文件解析
│   ├── ShareCodeUtil.cs      # 配装码工具
│   └── HashUtil.cs           # 哈希工具
├── Middleware/               # 中间件
│   └── JwtAuthMiddleware.cs  # JWT认证
└── Documents/                # 开发文档
    └── TacticsHall_Development.md  # 本文档
```

## 数据库设计

### 核心表

1. **user_level_config** - 用户等级配置
2. **tactics_user** - 用户表
3. **tactics_admin** - 管理员表
4. **tactics_file** - 战术文件主表
5. **tactics_file_version** - 文件版本表
6. **tactics_like** - 点赞表
7. **tactics_audit_log** - 审核记录表
8. **notification_config** - 通知配置表
9. **notification_log** - 通知日志表

详细设计见 `/home/hope_phenom/.claude/plans/sleepy-mixing-ripple.md`

## 开发阶段

### Phase 1: 基础架构与认证
- [ ] 数据库表创建（SQL脚本）
- [ ] 用户等级配置表初始化
- [ ] 数据模型（Entity Framework）
- [ ] TacticsDbContext
- [ ] JWT服务
- [ ] OAuth基础框架
- [ ] 开发模式登录
- [ ] AuthController
- [ ] 基础配置类

### Phase 2: 文件上传与安全 ✅ 已完成
- [x] 文件上传API
- [x] 文件安全验证（5层）
- [x] tactix文件解析
- [x] 配装码生成
- [x] 权限检查

### Phase 3: 版本管理与检索 🔄 进行中
- [ ] 版本链功能
- [ ] 多维度检索API
- [ ] 全文搜索
- [ ] 下载计数

### Phase 4: 审核机制
- [ ] 审核状态管理
- [ ] 管理员审核API
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

## 配置说明

### appsettings.json 新增配置

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

## 关键设计决策

### 1. 用户等级系统
- **normal**: 普通用户，批量通知
- **verified**: 认证作者，即时通知
- **pro**: 职业选手，即时通知
- **admin**: 管理员

### 2. 审核机制
- 所有上传必须经过审核
- normal用户：批量汇总通知（60分钟）
- verified/pro用户：即时通知

### 3. 文件安全
- 5层安全验证
- 临时目录 → 验证 → 正式目录
- 失败文件移至隔离区

### 4. 配装码
- 8位62进制编码
- 基于数据库ID转换
- 如：X7K9M2P3

## API概览

### 认证模块
- `GET /api/Auth/Login/{provider}` - 获取登录URL
- `GET /api/Auth/Callback/{provider}` - OAuth回调
- `POST /api/Auth/RefreshToken` - 刷新Token

### 战术大厅模块
- `POST /api/TacticsHall/Upload` - 上传战术文件
- `GET /api/TacticsHall/Download/{shareCode}` - 下载
- `GET /api/TacticsHall/Detail/{shareCode}` - 详情
- `GET /api/TacticsHall/Search` - 搜索
- `POST /api/TacticsHall/Like/{shareCode}` - 点赞

### 管理员模块
- `GET /api/Admin/PendingFiles` - 待审核列表
- `POST /api/Admin/Approve/{fileId}` - 通过审核
- `POST /api/Admin/Reject/{fileId}` - 拒绝审核

## 注意事项

1. **超级管理员**: `hope_phenom` 作为内置超级管理员
2. **QQ/微信OAuth**: 需开放平台申请AppID/AppSecret
3. **文件存储**: 需确保目录权限正确（读写执行）
4. **数据库**: 使用utf8mb4字符集支持emoji

## 开发日志

### 2025-03-29 - Phase 1 完成
- 创建功能分支 `features/tactics_hall`
- 创建开发文档
- 创建数据库SQL脚本 (`SQLScript/tactics_hall_tables.sql`)
- 创建数据模型：
  - UserLevelConfigModel - 用户等级配置
  - TacticsUserModel - 用户模型
  - TacticsAdminModel - 管理员模型
  - TacticsFileModel - 战术文件主表
  - TacticsFileVersionModel - 文件版本表
  - TacticsLikeModel - 点赞表
  - TacticsAuditLogModel - 审核日志表
  - NotificationConfigModel - 通知配置表
  - NotificationLogModel - 通知日志表
- 创建TacticsDbContext数据库上下文
- 创建JWT服务和配置
- 创建OAuth基础框架（IOAuthProvider接口）
- 创建开发模式登录服务（DevAuthService）
- 创建管理员权限服务（AdminService）
- 创建AuthController认证控制器
- 创建JWT认证中间件
- 更新Program.cs注册新服务
- 创建配置类（JwtConfig, TacticsHallConfig）

**已完成Phase 1全部内容**

### 2025-03-29 - Phase 2 完成
- 创建工具类:
  - ShareCodeUtil - 配装码生成与解析（62进制）
  - HashUtil - SHA256哈希计算
  - TacticsFileParser - tactix文件解析与验证
- 创建服务:
  - PermissionService - 权限检查服务
  - FileSecurityValidator - 5层文件安全验证
  - TacticsFileService - 战术文件上传/下载/删除服务
- 创建请求/响应模型:
  - UploadTacticsRequest, UploadVersionRequest, SearchTacticsRequest
  - UploadTacticsResponse, TacticsDetailResponse等
- 创建TacticsHallController控制器:
  - Upload - 上传战术文件
  - UploadVersion - 上传新版本
  - GetDetail - 获取文件详情
  - Search - 搜索战术文件
  - Download - 下载文件
  - GetVersions - 获取版本列表
  - Delete - 删除文件
  - Like - 点赞功能
- 更新Program.cs注册新服务

**已完成Phase 2全部内容**
