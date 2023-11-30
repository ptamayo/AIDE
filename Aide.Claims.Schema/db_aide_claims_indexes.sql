USE `db_aide_claims`;

ALTER TABLE `claim` 
ADD INDEX `IX_CLAIM_CLAIM_STATUS_ID` (`claim_status_id` ASC) VISIBLE;

ALTER TABLE `claim_probatory_document` 
ADD INDEX `IX_CLAIM_ID` (`claim_id` ASC) VISIBLE;

ALTER TABLE `claim_probatory_document_media` 
ADD INDEX `IX_CLAIM_PROBATORY_DOCUMENT_ID` (`claim_probatory_document_id` ASC) VISIBLE;

ALTER TABLE `claim_signature` 
ADD INDEX `IX_CLAIM_ID` (`claim_id` ASC) VISIBLE;

ALTER TABLE `claim_document` 
ADD INDEX `IX_CLAIM_ID` (`claim_id` ASC) VISIBLE;
