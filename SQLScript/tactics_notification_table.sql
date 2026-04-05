-- 通知记录表
-- 用于存储用户通知（审核结果、评论回复、点赞提醒等）

USE `tactix`;

CREATE TABLE IF NOT EXISTS `tactics_notification` (
    `id` BIGINT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    `user_id` BIGINT UNSIGNED NOT NULL COMMENT '接收通知的用户ID',
    `type` VARCHAR(32) NOT NULL COMMENT '通知类型',
    `title` VARCHAR(255) NOT NULL COMMENT '通知标题',
    `content` TEXT COMMENT '通知内容',
    `related_file_id` BIGINT UNSIGNED COMMENT '关联文件ID',
    `related_comment_id` BIGINT UNSIGNED COMMENT '关联评论ID',
    `related_user_id` BIGINT UNSIGNED COMMENT '触发通知的用户ID',
    `is_read` TINYINT(1) DEFAULT 0 COMMENT '是否已读',
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    KEY `idx_user_id` (`user_id`) COMMENT '按用户查询',
    KEY `idx_user_unread` (`user_id`, `is_read`) COMMENT '按用户未读查询',
    KEY `idx_type` (`type`) COMMENT '按类型查询',
    KEY `idx_created_at` (`created_at`) COMMENT '按时间排序',
    CONSTRAINT `fk_notification_user` FOREIGN KEY (`user_id`)
        REFERENCES `tactics_user`(`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_notification_file` FOREIGN KEY (`related_file_id`)
        REFERENCES `tactics_file`(`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_notification_comment` FOREIGN KEY (`related_comment_id`)
        REFERENCES `tactics_comment`(`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_notification_related_user` FOREIGN KEY (`related_user_id`)
        REFERENCES `tactics_user`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='通知记录表';