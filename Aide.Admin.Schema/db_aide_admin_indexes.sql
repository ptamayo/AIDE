USE `db_aide_admin`;

ALTER TABLE `user_psw_history` 
ADD INDEX `ix_user_id` (`user_id` ASC) VISIBLE;

ALTER TABLE `user_psw_history` 
ADD INDEX `ix_user_id_psw` (`user_id` ASC,`psw` ASC) VISIBLE;
