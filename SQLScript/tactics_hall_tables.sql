-- --------------------------------------------------------
-- TactiX 战术大厅数据库脚本
-- 版本: 1.0.0
-- --------------------------------------------------------

-- 使用tactix数据库
USE `tactix`;

-- --------------------------------------------------------
-- 1. 用户等级配置表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `user_level_config` (
    `id` INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `level_code` VARCHAR(32) NOT NULL COMMENT '等级代码: normal/verified/pro/admin',
    `level_name` VARCHAR(64) NOT NULL COMMENT '等级名称',
    `description` TEXT COMMENT '等级描述',

    -- 上传限制
    `max_file_size` INT UNSIGNED NOT NULL DEFAULT 10485760 COMMENT '单文件最大大小(字节)',
    `max_upload_count` INT UNSIGNED NOT NULL DEFAULT 10 COMMENT '允许上传的战术文件总数',
    `max_version_per_file` INT UNSIGNED NOT NULL DEFAULT 5 COMMENT '单个战术文件最大版本数',
    `daily_upload_limit` INT UNSIGNED NOT NULL DEFAULT 3 COMMENT '每日上传限制',

    -- 功能权限
    `can_upload` TINYINT(1) DEFAULT 1 COMMENT '是否允许上传',
    `can_delete_own_file` TINYINT(1) DEFAULT 1 COMMENT '是否可删除自己的文件',
    `can_comment` TINYINT(1) DEFAULT 1 COMMENT '是否允许评论',
    `priority_review` TINYINT(1) DEFAULT 0 COMMENT '是否优先审核',
    `instant_notification` TINYINT(1) DEFAULT 0 COMMENT '审核通知方式：1-即时通知，0-批量汇总',

    -- UI展示
    `badge_icon` VARCHAR(255) COMMENT '等级徽章图标URL',
    `badge_color` VARCHAR(32) COMMENT '等级徽章颜色',
    `show_in_leaderboard` TINYINT(1) DEFAULT 1 COMMENT '是否在排行榜展示',

    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_level_code` (`level_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='用户等级配置表';

-- 初始化等级配置数据
INSERT INTO `user_level_config`
(`level_code`, `level_name`, `description`, `max_file_size`, `max_upload_count`, `max_version_per_file`,
`daily_upload_limit`, `can_upload`, `can_delete_own_file`, `can_comment`, `priority_review`,
`instant_notification`, `badge_color`) VALUES
('normal', '普通用户', '普通用户，基础权限', 10485760, 10, 5, 3, 1, 1, 1, 0, 0, '#95a5a6'),
('verified', '认证作者', '认证作者，更高权限', 20971520, 50, 10, 10, 1, 1, 1, 1, 1, '#3498db'),
('pro', '职业选手', '职业选手，最高权限', 52428800, 200, 20, 50, 1, 1, 1, 1, 1, '#e74c3c'),
('admin', '管理员', '系统管理员', 104857600, 1000, 50, 100, 1, 1, 1, 1, 1, '#2ecc71');

-- --------------------------------------------------------
-- 2. 用户表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_user` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `oauth_provider` VARCHAR(16) NOT NULL COMMENT 'OAuth提供商: qq/wechat/dev',
    `oauth_id` VARCHAR(64) NOT NULL COMMENT 'OAuth平台用户唯一ID',
    `level_code` VARCHAR(32) NOT NULL DEFAULT 'normal' COMMENT '用户等级',
    `nickname` VARCHAR(64) COMMENT '昵称',
    `avatar_url` VARCHAR(255) COMMENT '头像URL',
    `bio` TEXT COMMENT '个人简介',
    `is_active` TINYINT(1) DEFAULT 1 COMMENT '账号是否激活',

    -- 统计信息
    `upload_count` INT UNSIGNED DEFAULT 0 COMMENT '已上传文件数',
    `total_download_count` INT UNSIGNED DEFAULT 0 COMMENT '总下载次数',
    `total_like_count` INT UNSIGNED DEFAULT 0 COMMENT '总点赞数',

    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_oauth` (`oauth_provider`, `oauth_id`),
    KEY `idx_level_code` (`level_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='战术大厅用户表';

-- --------------------------------------------------------
-- 3. 管理员表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_admin` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT UNSIGNED NOT NULL COMMENT '关联的用户ID',
    `role` ENUM('super_admin', 'admin', 'moderator') DEFAULT 'moderator' COMMENT '管理员角色',
    `granted_by` BIGINT UNSIGNED COMMENT '授予者ID',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_user_id` (`user_id`),
    KEY `idx_role` (`role`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='管理员表';

-- --------------------------------------------------------
-- 4. 战术文件主表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_file` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `share_code` VARCHAR(8) NOT NULL COMMENT '8位配装码',
    `uploader_id` BIGINT UNSIGNED NOT NULL COMMENT '上传者ID',
    `name` VARCHAR(128) NOT NULL COMMENT '战术名称',
    `description` TEXT COMMENT '战术描述',
    `author_name` VARCHAR(64) COMMENT '作者名称（文件内）',
    `mod_name` VARCHAR(32) NOT NULL DEFAULT 'StarCraft2' COMMENT '游戏模组',
    `tactic_type` TINYINT UNSIGNED DEFAULT 0 COMMENT '战术风格类型',
    `race_played` ENUM('P','T','Z','Unknown') COMMENT '使用种族',
    `race_opponent` ENUM('P','T','Z','Unknown') COMMENT '对手种族',
    `matchup` VARCHAR(8) COMMENT '对抗类型，如PvT、PvZ等',
    `file_path` VARCHAR(255) NOT NULL COMMENT '文件存储路径',
    `file_size` INT UNSIGNED NOT NULL COMMENT '文件大小(字节)',
    `file_hash` VARCHAR(64) NOT NULL COMMENT '文件SHA256哈希值',
    `download_count` INT UNSIGNED DEFAULT 0 COMMENT '下载次数',
    `like_count` INT UNSIGNED DEFAULT 0 COMMENT '点赞数',
    `status` ENUM('pending','approved','rejected') DEFAULT 'pending' COMMENT '审核状态',
    `is_latest_version` TINYINT(1) DEFAULT 1 COMMENT '是否最新版本',
    `latest_version_id` BIGINT UNSIGNED COMMENT '最新版本ID',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_share_code` (`share_code`),
    KEY `idx_status` (`status`),
    KEY `idx_race_played` (`race_played`),
    KEY `idx_race_opponent` (`race_opponent`),
    KEY `idx_matchup` (`matchup`),
    KEY `idx_tactic_type` (`tactic_type`),
    KEY `idx_uploader` (`uploader_id`),
    FULLTEXT KEY `ft_name_desc` (`name`, `description`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='战术文件主表';

-- --------------------------------------------------------
-- 5. 战术文件版本表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_file_version` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `file_id` BIGINT UNSIGNED NOT NULL COMMENT '关联的战术文件ID',
    `version_number` INT UNSIGNED NOT NULL COMMENT '版本号(1,2,3...)',
    `tac_version` INT UNSIGNED NOT NULL COMMENT 'tactix文件内的版本号',
    `file_path` VARCHAR(255) NOT NULL COMMENT '该版本的文件路径',
    `file_size` INT UNSIGNED NOT NULL,
    `file_hash` VARCHAR(64) NOT NULL COMMENT '文件SHA256哈希值',
    `changelog` TEXT COMMENT '版本更新说明',
    `is_deleted` TINYINT(1) DEFAULT 0 COMMENT '是否已删除',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_file_version` (`file_id`, `version_number`),
    KEY `idx_file_id` (`file_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='战术文件版本表';

-- --------------------------------------------------------
-- 6. 用户点赞表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_like` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT UNSIGNED NOT NULL,
    `file_id` BIGINT UNSIGNED NOT NULL,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_user_file` (`user_id`, `file_id`),
    KEY `idx_file_id` (`file_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='用户点赞表';

-- --------------------------------------------------------
-- 7. 审核记录表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_audit_log` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `file_id` BIGINT UNSIGNED NOT NULL,
    `admin_id` BIGINT UNSIGNED NOT NULL COMMENT '审核管理员ID',
    `old_status` ENUM('pending','approved','rejected'),
    `new_status` ENUM('pending','approved','rejected'),
    `reason` TEXT COMMENT '审核意见',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_file_id` (`file_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='审核记录表';

-- --------------------------------------------------------
-- 8. 通知配置表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `notification_config` (
    `id` INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `config_key` VARCHAR(64) NOT NULL COMMENT '配置键',
    `config_value` TEXT COMMENT '配置值',
    `description` TEXT COMMENT '配置说明',
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_config_key` (`config_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='通知配置表';

-- 初始化通知配置
INSERT INTO `notification_config` (`config_key`, `config_value`, `description`) VALUES
('email.smtp_server', '', 'SMTP服务器地址'),
('email.smtp_port', '587', 'SMTP端口'),
('email.username', '', '邮箱用户名'),
('email.password', '', '邮箱密码'),
('email.from', '', '发件人地址'),
('feishu.webhook_url', '', '飞书机器人WebHook地址'),
('feishu.secret', '', '飞书机器人密钥'),
('alert.storage_threshold', '85', '存储空间告警阈值(%)'),
('alert.email_enabled', '1', '是否启用邮件通知'),
('alert.feishu_enabled', '1', '是否启用飞书通知'),
('digest.pending_audit_interval', '60', '待审核汇总通知间隔(分钟)'),
('digest.email_enabled', '1', '是否启用待审核邮件汇总'),
('digest.feishu_enabled', '1', '是否启用待审核飞书汇总');

-- --------------------------------------------------------
-- 9. 系统通知日志表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `notification_log` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `notification_type` ENUM('instant', 'digest') NOT NULL COMMENT '通知类型',
    `event_type` VARCHAR(64) NOT NULL COMMENT '事件类型',
    `channel` ENUM('email', 'feishu') NOT NULL COMMENT '通知渠道',
    `title` VARCHAR(255) NOT NULL COMMENT '通知标题',
    `content` TEXT NOT NULL COMMENT '通知内容',
    `status` ENUM('pending', 'sent', 'failed') DEFAULT 'pending' COMMENT '发送状态',
    `error_message` TEXT COMMENT '错误信息',
    `sent_at` DATETIME COMMENT '发送时间',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    KEY `idx_type` (`notification_type`, `event_type`),
    KEY `idx_status` (`status`),
    KEY `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='系统通知日志表';

-- --------------------------------------------------------
-- 添加外键约束（可选，根据需要启用）
-- --------------------------------------------------------
-- ALTER TABLE `tactics_user` ADD CONSTRAINT `fk_user_level`
--     FOREIGN KEY (`level_code`) REFERENCES `user_level_config`(`level_code`);

-- ALTER TABLE `tactics_file` ADD CONSTRAINT `fk_file_uploader`
--     FOREIGN KEY (`uploader_id`) REFERENCES `tactics_user`(`id`);

-- ALTER TABLE `tactics_file_version` ADD CONSTRAINT `fk_version_file`
--     FOREIGN KEY (`file_id`) REFERENCES `tactics_file`(`id`);

-- ALTER TABLE `tactics_like` ADD CONSTRAINT `fk_like_user`
--     FOREIGN KEY (`user_id`) REFERENCES `tactics_user`(`id`);

-- ALTER TABLE `tactics_like` ADD CONSTRAINT `fk_like_file`
--     FOREIGN KEY (`file_id`) REFERENCES `tactics_file`(`id`);

-- ALTER TABLE `tactics_admin` ADD CONSTRAINT `fk_admin_user`
--     FOREIGN KEY (`user_id`) REFERENCES `tactics_user`(`id`);
