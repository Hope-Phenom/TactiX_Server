-- 举报记录表
-- 用于用户举报违规战术文件，管理员处理举报

USE `tactix`;

CREATE TABLE IF NOT EXISTS `tactics_report` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `file_id` BIGINT UNSIGNED NOT NULL COMMENT '被举报的文件ID',
    `reporter_id` BIGINT UNSIGNED NOT NULL COMMENT '举报用户ID',
    `reason` VARCHAR(32) NOT NULL COMMENT '举报原因类型',
    `description` TEXT COMMENT '举报详细描述',
    `status` VARCHAR(16) NOT NULL DEFAULT 'pending' COMMENT '处理状态: pending/processed/ignored',
    `handled_by` BIGINT UNSIGNED COMMENT '处理管理员ID',
    `handle_result` TEXT COMMENT '处理结果说明',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    UNIQUE KEY `uk_file_reporter` (`file_id`, `reporter_id`) COMMENT '防止重复举报',
    KEY `idx_file_id` (`file_id`) COMMENT '按文件查询举报',
    KEY `idx_reporter_id` (`reporter_id`) COMMENT '按举报者查询',
    KEY `idx_status` (`status`) COMMENT '按状态查询',
    KEY `idx_created_at` (`created_at`) COMMENT '按时间排序',
    CONSTRAINT `fk_report_file` FOREIGN KEY (`file_id`)
        REFERENCES `tactics_file`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_report_reporter` FOREIGN KEY (`reporter_id`)
        REFERENCES `tactics_user`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_report_handler` FOREIGN KEY (`handled_by`)
        REFERENCES `tactics_admin`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='举报记录表';