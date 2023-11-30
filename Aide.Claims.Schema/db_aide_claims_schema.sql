CREATE DATABASE `db_aide_claims` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `db_aide_claims`;

CREATE TABLE `claim` (
  `claim_id` int NOT NULL AUTO_INCREMENT,
  `claim_status_id` int NOT NULL,
  `claim_type_id` int NOT NULL,
  `customer_full_name` varchar(150) NOT NULL,
  `policy_number` VARCHAR(50) DEFAULT NULL,
  `policy_subsection` VARCHAR(50) DEFAULT NULL,
  `claim_number` varchar(50) DEFAULT NULL,
  `report_number` VARCHAR(50) DEFAULT NULL,
  `external_order_number` varchar(50) DEFAULT NULL,
  `insurance_company_id` int NOT NULL,
  `store_id` int NOT NULL,
  `claim_probatory_document_status_id` int NOT NULL,
  `is_deposit_slip_required` TINYINT(1) NOT NULL,
  `has_deposit_slip` tinyint(1) NOT NULL,
  `items_quantity` INT NOT NULL,
  `source` VARCHAR(5) DEFAULT NULL,
  `created_by_user_id` int NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`claim_id`)
);

CREATE TABLE `claim_status` (
  `claim_status_id` int NOT NULL,
  `claim_status_name` varchar(45) NOT NULL,
  PRIMARY KEY (`claim_status_id`)
);

CREATE TABLE `claim_document` (
  `claim_document_id` int NOT NULL AUTO_INCREMENT,
  `claim_document_type_id` int NOT NULL,
  `claim_document_status_id` int NOT NULL,
  `claim_id` int NOT NULL,
  `document_id` int NOT NULL,
  `sort_priority` int NOT NULL,
  `group_id` int NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`claim_document_id`)
);

CREATE TABLE `claim_document_type` (
  `claim_document_type_id` int NOT NULL AUTO_INCREMENT,
  `document_type_id` int NOT NULL,
  `sort_priority` int NOT NULL,
  `group_id` int NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`claim_document_type_id`)
);

CREATE TABLE `claim_probatory_document` (
  `claim_probatory_document_id` int NOT NULL AUTO_INCREMENT,
  `claim_id` int NOT NULL,
  `claim_item_id` int DEFAULT NULL,
  `probatory_document_id` int NOT NULL,
  `sort_priority` int NOT NULL,
  `group_id` int NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`claim_probatory_document_id`)
);

CREATE TABLE `claim_probatory_document_media` (
  `claim_probatory_document_media_id` int NOT NULL AUTO_INCREMENT,
  `claim_probatory_document_id` int NOT NULL,
  `media_id` int NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`claim_probatory_document_media_id`)
);

CREATE TABLE `claim_signature` (
  `claim_signature_id` int NOT NULL AUTO_INCREMENT,
  `claim_id` int NOT NULL,
  `signature` longtext NOT NULL,
  `local_date` datetime NOT NULL,
  `local_timezone` varchar(50) NOT NULL,
  `date_created` datetime NOT NULL,
  PRIMARY KEY (`claim_signature_id`)
);

CREATE TABLE `claim_type` (
  `claim_type_id` int NOT NULL,
  `claim_type_name` varchar(50) NOT NULL,
  `sort_priority` int NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`claim_type_id`)
);

CREATE TABLE `document` (
  `document_id` int NOT NULL AUTO_INCREMENT,
  `mime_type` varchar(50) DEFAULT NULL,
  `filename` varchar(1024) NOT NULL,
  `url` varchar(1024) DEFAULT NULL,
  `metadata_title` varchar(250) DEFAULT NULL,
  `metadata_alt` varchar(250) DEFAULT NULL,
  `metadata_copyright` varchar(250) DEFAULT NULL,
  `checksum_sha1` varchar(40) DEFAULT NULL,
  `checksum_md5` varchar(32) DEFAULT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`document_id`)
);

CREATE TABLE `document_type` (
  `document_type_id` int NOT NULL,
  `document_type_name` varchar(150) NOT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`document_type_id`)
);

CREATE TABLE `media` (
  `media_id` int NOT NULL AUTO_INCREMENT,
  `mime_type` varchar(50) DEFAULT NULL,
  `filename` varchar(1024) NOT NULL,
  `url` varchar(1024) DEFAULT NULL,
  `metadata_title` varchar(250) DEFAULT NULL,
  `metadata_alt` varchar(250) DEFAULT NULL,
  `metadata_copyright` varchar(250) DEFAULT NULL,
  `checksum_sha1` varchar(40) DEFAULT NULL,
  `checksum_md5` varchar(32) DEFAULT NULL,
  `date_created` datetime NOT NULL,
  `date_modified` datetime NOT NULL,
  PRIMARY KEY (`media_id`)
);
