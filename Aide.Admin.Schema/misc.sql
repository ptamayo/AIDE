USE `db_aide_admin`;

select * from user;
select * from user_company;
select * from probatory_document;
select * from insurance_company;
select * from insurance_probatory_document x where x.probatory_document_id=8;#15
select * from store;

#insert into insurance_probatory_document_bak
#select * from insurance_probatory_document;