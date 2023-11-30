CREATE DATABASE `db_aide_admin` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `db_aide_admin`;

CREATE TABLE `insurance_company` (
  `insurance_company_id` int NOT NULL AUTO_INCREMENT,
  `insurance_company_name` varchar(250) NOT NULL,
  `is_enabled` tinyint(1) NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`insurance_company_id`)
);

CREATE TABLE `insurance_company_claim_type_settings` (
  `insurance_company_id` INT NOT NULL,
  `claim_type_id` INT NOT NULL,
  `is_enabled` TINYINT(1) NOT NULL,
  `is_deposit_slip_required` TINYINT(1) NOT NULL,
  `is_exporting_customized_docs_to_pdf` TINYINT(1) NOT NULL,
  `is_exporting_customized_docs_to_zip` TINYINT(1) NOT NULL,
  `date_created` DATETIME NOT NULL,
  `date_modified` DATETIME NOT NULL,
  PRIMARY KEY (`insurance_company_id`, `claim_type_id`));

CREATE TABLE `insurance_probatory_document` (
  `insurance_probatory_document_id` int NOT NULL AUTO_INCREMENT,
  `insurance_company_id` int NOT NULL,
  `claim_type_id` int NOT NULL,
  `probatory_document_id` int NOT NULL,
  `sort_priority` int NOT NULL,
  `group_id` int NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`insurance_probatory_document_id`)
);

CREATE TABLE `insurance_export_probatory_document` (
  `insurance_export_probatory_document_id` int NOT NULL AUTO_INCREMENT,
  `export_type_id` int NOT NULL,
  `insurance_company_id` int NOT NULL,
  `claim_type_id` int NOT NULL,
  `export_document_type_id` int NOT NULL,
  `sort_priority` int NOT NULL,
  `probatory_document_id` int DEFAULT NULL,
  `collage_id` int DEFAULT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`insurance_export_probatory_document_id`)
);


CREATE TABLE `probatory_document` (
  `probatory_document_id` int NOT NULL AUTO_INCREMENT,
  `probatory_document_name` varchar(250) NOT NULL,
  `probatory_document_orientation` INT NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`probatory_document_id`)
);

CREATE TABLE `store` (
  `store_id` int NOT NULL AUTO_INCREMENT,
  `store_name` varchar(250) NOT NULL,
  `store_sap_number` varchar(15) DEFAULT NULL,
  `store_email` varchar(100) DEFAULT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`store_id`)
);

CREATE TABLE `user` (
  `user_id` int NOT NULL AUTO_INCREMENT,
  `role_id` int NOT NULL,
  `user_first_name` varchar(50) NOT NULL,
  `user_last_name` varchar(50) NOT NULL,
  `email` varchar(100) NOT NULL,
  `psw` varchar(128) NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  `date_logout` datetime NOT NULL,
  `last_login_attempt` int NULL,
  `time_last_attempt` datetime NULL,
  PRIMARY KEY (`user_id`)
);

CREATE TABLE `user_company` (
  `user_company_id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `company_id` int NOT NULL,
  `company_type_id` int NOT NULL,
  `date_created` datetime NOT NULL,
  PRIMARY KEY (`user_company_id`)
);

CREATE TABLE `user_psw_history` (
  `user_psw_history_id` int NOT NULL AUTO_INCREMENT,
  `user_id` int NOT NULL,
  `psw` varchar(128) NOT NULL,
  `date_created` datetime NOT NULL,
  PRIMARY KEY (`user_psw_history_id`)
);


CREATE TABLE `insurance_collage` (
  `insurance_collage_id` INT NOT NULL AUTO_INCREMENT,
  `insurance_collage_name` VARCHAR(250) NOT NULL,
  `insurance_company_id` INT NOT NULL,
  `claim_type_id` INT NOT NULL,
  `columns` INT NOT NULL,
  `date_created` DATETIME NOT NULL,
  `date_modified` DATETIME NOT NULL,
  PRIMARY KEY (`insurance_collage_id`)
);

CREATE TABLE `insurance_collage_probatory_document` (
  `insurance_collage_probatory_document_id` INT NOT NULL AUTO_INCREMENT,
  `insurance_collage_id` INT NOT NULL,
  `probatory_document_id` INT NOT NULL,
  `sort_priority` INT NOT NULL,
  `date_created` DATETIME NOT NULL,
  `date_modified` DATETIME NOT NULL,
  PRIMARY KEY (`insurance_collage_probatory_document_id`)
);
