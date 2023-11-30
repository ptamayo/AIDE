ALTER TABLE `db_aide_hub` 
ADD INDEX `IX_USER_AND_NOTIFICATION_IDS` (`user_id` ASC, `notification_id` ASC) VISIBLE;
