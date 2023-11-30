USE `db_aide_claims`;

select * from claim_type;
select * from claim;
select * from claim_probatory_document;
select * from claim_probatory_document_media;
select * from media; # Need automated cleansing: Delete rows that don't exist in table 'claim_probatory_document_media', also delete physical files (if exist)
select * from claim_document;
select * from document; # Need automated cleansing: Delete rows that don't exist in table 'claim_document', also delete physical files (if exist)
select * from document_type;
select * from claim_document_type;
select * from claim_signature;

/*
truncate table claim;
truncate table claim_probatory_document;
truncate table claim_probatory_document_media;
truncate table media;
truncate table claim_signature;
truncate table claim_document;
truncate table document;
*/
