-- --------------------------------------------------------
-- TactiX 战术大厅数据库脚本
-- Milestone 2: 用户表和管理员表
-- 版本: 1.1.0
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
    KEY `idx_level_code` (`level_code`),
    CONSTRAINT `fk_user_level` FOREIGN KEY (`level_code`) REFERENCES `user_level_config`(`level_code`) ON UPDATE CASCADE
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
    KEY `idx_role` (`role`),
    CONSTRAINT `fk_admin_user` FOREIGN KEY (`user_id`) REFERENCES `tactics_user`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='管理员表';

-- --------------------------------------------------------
-- 初始化数据
-- --------------------------------------------------------

-- 初始化等级配置数据
INSERT INTO `user_level_config`
(`id`, `level_code`, `level_name`, `description`, `max_file_size`, `max_upload_count`, `max_version_per_file`,
`daily_upload_limit`, `can_upload`, `can_delete_own_file`, `can_comment`, `priority_review`,
`instant_notification`, `badge_color`) VALUES
(1, 'normal', '普通用户', '普通用户，基础权限', 10485760, 10, 5, 3, 1, 1, 1, 0, 0, '#95a5a6'),
(2, 'verified', '认证作者', '认证作者，更高权限', 20971520, 50, 10, 10, 1, 1, 1, 1, 1, '#3498db'),
(3, 'pro', '职业选手', '职业选手，最高权限', 52428800, 200, 20, 50, 1, 1, 1, 1, 1, '#e74c3c'),
(4, 'admin', '管理员', '系统管理员', 104857600, 1000, 50, 100, 1, 1, 1, 1, 1, '#2ecc71')
ON DUPLICATE KEY UPDATE `updated_at` = CURRENT_TIMESTAMP;

-- --------------------------------------------------------
-- 4. 战术文件表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_file` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `share_code` VARCHAR(8) NOT NULL COMMENT '8字符配装码',
    `name` VARCHAR(255) COMMENT '战术名称',
    `author` VARCHAR(128) COMMENT '作者名称',
    `race` VARCHAR(1) COMMENT '种族代码: P/T/Z',
    `uploader_id` BIGINT UNSIGNED NOT NULL COMMENT '上传用户ID',
    `file_path` VARCHAR(512) NOT NULL COMMENT '文件存储路径',
    `file_hash` VARCHAR(64) NOT NULL COMMENT 'SHA256哈希',
    `file_size` BIGINT UNSIGNED NOT NULL COMMENT '文件大小(字节)',
    `version` INT UNSIGNED DEFAULT 1 COMMENT '当前版本号',
    `original_file_id` BIGINT UNSIGNED COMMENT '原始文件ID(版本追溯)',
    `latest_version_id` BIGINT UNSIGNED COMMENT '最新版本ID',
    `status` VARCHAR(16) NOT NULL DEFAULT 'pending' COMMENT '审核状态: pending/approved/rejected',
    `download_count` INT UNSIGNED DEFAULT 0 COMMENT '下载次数',
    `like_count` INT UNSIGNED DEFAULT 0 COMMENT '点赞次数',
    `is_public` TINYINT(1) DEFAULT 1 COMMENT '是否公开',
    `is_deleted` TINYINT(1) DEFAULT 0 COMMENT '是否删除',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_share_code` (`share_code`),
    KEY `idx_uploader_id` (`uploader_id`),
    KEY `idx_race` (`race`),
    KEY `idx_status` (`status`),
    KEY `idx_file_hash` (`file_hash`),
    CONSTRAINT `fk_file_uploader` FOREIGN KEY (`uploader_id`) REFERENCES `tactics_user`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='战术文件表';

-- --------------------------------------------------------
-- 5. 战术文件版本表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_file_version` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `file_id` BIGINT UNSIGNED NOT NULL COMMENT '关联文件ID',
    `version_number` INT UNSIGNED NOT NULL COMMENT '版本号(从1开始)',
    `file_path` VARCHAR(512) NOT NULL COMMENT '文件存储路径',
    `file_hash` VARCHAR(64) NOT NULL COMMENT 'SHA256哈希',
    `file_size` BIGINT UNSIGNED NOT NULL COMMENT '文件大小(字节)',
    `changelog` TEXT COMMENT '版本更新说明',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY `uk_file_version` (`file_id`, `version_number`),
    KEY `idx_file_id` (`file_id`),
    KEY `idx_file_hash` (`file_hash`),
    CONSTRAINT `fk_version_file` FOREIGN KEY (`file_id`) REFERENCES `tactics_file`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='战术文件版本表';

-- --------------------------------------------------------
-- 6. 点赞记录表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_like` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT UNSIGNED NOT NULL COMMENT '点赞用户ID',
    `file_id` BIGINT UNSIGNED NOT NULL COMMENT '点赞的文件ID',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '点赞时间',
    UNIQUE KEY `uk_user_file` (`user_id`, `file_id`),
    KEY `idx_file_id` (`file_id`),
    CONSTRAINT `fk_like_user` FOREIGN KEY (`user_id`) REFERENCES `tactics_user`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_like_file` FOREIGN KEY (`file_id`) REFERENCES `tactics_file`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='点赞记录表';

-- --------------------------------------------------------
-- 7. 收藏记录表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_favorite` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT UNSIGNED NOT NULL COMMENT '收藏用户ID',
    `file_id` BIGINT UNSIGNED NOT NULL COMMENT '收藏的文件ID',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '收藏时间',
    UNIQUE KEY `uk_user_file` (`user_id`, `file_id`),
    KEY `idx_user_id` (`user_id`),
    KEY `idx_file_id` (`file_id`),
    CONSTRAINT `fk_favorite_user` FOREIGN KEY (`user_id`) REFERENCES `tactics_user`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_favorite_file` FOREIGN KEY (`file_id`) REFERENCES `tactics_file`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='收藏记录表';

-- --------------------------------------------------------
-- 8. 评论表
-- --------------------------------------------------------
CREATE TABLE IF NOT EXISTS `tactics_comment` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT UNSIGNED NOT NULL COMMENT '评论用户ID',
    `file_id` BIGINT UNSIGNED NOT NULL COMMENT '评论的文件ID',
    `parent_comment_id` BIGINT UNSIGNED COMMENT '父评论ID(用于嵌套回复)',
    `content` VARCHAR(1000) NOT NULL COMMENT '评论内容',
    `is_deleted` TINYINT(1) DEFAULT 0 COMMENT '是否删除(软删除)',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    KEY `idx_file_id` (`file_id`),
    KEY `idx_user_id` (`user_id`),
    KEY `idx_parent_id` (`parent_comment_id`),
    CONSTRAINT `fk_comment_user` FOREIGN KEY (`user_id`) REFERENCES `tactics_user`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_comment_file` FOREIGN KEY (`file_id`) REFERENCES `tactics_file`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_comment_parent` FOREIGN KEY (`parent_comment_id`) REFERENCES `tactics_comment`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='评论表';

-- --------------------------------------------------------
-- 数据库升级：添加favorite_count字段
-- --------------------------------------------------------
-- 为已存在的tactics_file表添加favorite_count字段
-- 注意：如果字段已存在会报错，可忽略该错误
ALTER TABLE `tactics_file` ADD COLUMN `favorite_count` INT UNSIGNED DEFAULT 0 COMMENT '收藏次数' AFTER `like_count`;