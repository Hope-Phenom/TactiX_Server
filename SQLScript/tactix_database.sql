-- --------------------------------------------------------
-- 主机:                           127.0.0.1
-- 服务器版本:                        8.0.43 - MySQL Community Server - GPL
-- 服务器操作系统:                      Win64
-- HeidiSQL 版本:                  12.11.0.7065
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- 导出 tactix 的数据库结构
CREATE DATABASE IF NOT EXISTS `tactix` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;
USE `tactix`;

-- 导出  表 tactix.config_video_up 结构
CREATE TABLE IF NOT EXISTS `config_video_up` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT '主键',
  `name` text NOT NULL COMMENT '名称',
  `type` tinyint unsigned NOT NULL DEFAULT (0) COMMENT '类型，0-Bilibili',
  `url` text NOT NULL COMMENT '地址',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='视频博主的地址配置，用于从中解析要推荐的视频';

-- 数据导出被取消选择。

-- 导出  表 tactix.news_community 结构
CREATE TABLE IF NOT EXISTS `news_community` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT '主键',
  `update_datetime` datetime NOT NULL COMMENT '更新时间',
  `json` json NOT NULL COMMENT '新闻列表JSON',
  `type` tinyint unsigned NOT NULL COMMENT '类别，0-社区热帖，1-Bilibili视频推荐',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=75 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci MAX_ROWS=1000 COMMENT='社区热帖和Bilibili视频推荐';

-- 数据导出被取消选择。

-- 导出  表 tactix.news_sys 结构
CREATE TABLE IF NOT EXISTS `news_sys` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT '主键',
  `title` tinytext NOT NULL COMMENT '标题',
  `link` text NOT NULL COMMENT '跳转链接',
  `datetime` datetime NOT NULL COMMENT '日期',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='系统公告';

-- 数据导出被取消选择。

-- 导出  表 tactix.stats_excption_report 结构
CREATE TABLE IF NOT EXISTS `stats_excption_report` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT '索引',
  `error_code` smallint unsigned NOT NULL DEFAULT (0) COMMENT '错误代码',
  `error_desc` text NOT NULL COMMENT '错误信息',
  `feedback_way` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL COMMENT '反馈渠道',
  `feedback_info` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci COMMENT '反馈信息',
  `create_time` datetime NOT NULL COMMENT '报告创建日期',
  `is_read` tinyint unsigned NOT NULL DEFAULT '0' COMMENT '是否已读',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='异常上报统计';

-- 数据导出被取消选择。

-- 导出  表 tactix.stats_version_control 结构
CREATE TABLE IF NOT EXISTS `stats_version_control` (
  `id` int unsigned NOT NULL AUTO_INCREMENT COMMENT 'id',
  `version` varchar(255) NOT NULL COMMENT '版本号',
  `banned` tinyint unsigned NOT NULL DEFAULT (0) COMMENT '版本是否被禁用',
  `force_upgrade` tinyint unsigned NOT NULL DEFAULT (0) COMMENT '是否需要强制更新到此版本',
  `release_time` datetime NOT NULL COMMENT '版本发布时间',
  `release_url` text NOT NULL COMMENT '版本发布位置',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci COMMENT='程序版本控制表';

-- 数据导出被取消选择。

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;

-- 添加性能优化索引（兼容MySQL 5.7+）
DROP INDEX IF EXISTS idx_news_community_type ON news_community;
CREATE INDEX idx_news_community_type ON news_community(type);
DROP INDEX IF EXISTS idx_news_community_update_datetime ON news_community;
CREATE INDEX idx_news_community_update_datetime ON news_community(update_datetime);
DROP INDEX IF EXISTS idx_news_sys_datetime ON news_sys;
CREATE INDEX idx_news_sys_datetime ON news_sys(datetime);
DROP INDEX IF EXISTS idx_stats_version_control_version ON stats_version_control;
CREATE INDEX idx_stats_version_control_version ON stats_version_control(version);
DROP INDEX IF EXISTS idx_stats_excption_report_create_time ON stats_excption_report;
CREATE INDEX idx_stats_excption_report_create_time ON stats_excption_report(create_time);
