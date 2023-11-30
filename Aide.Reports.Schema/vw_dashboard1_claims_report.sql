USE `db_aide_reports`;

CREATE OR REPLACE VIEW `vw_dashboard1_claims_report` AS
select
	i.insurance_company_id,
	i.insurance_company_name, -- as `Aseguradora`,
    s.store_id,
	s.store_sap_number, -- as `?`
    s.store_name, -- as `Taller`,
    c.claim_status_id,
    case c.claim_status_id
		when 10 then 'En Proceso'
        when 20 then 'Completado'
        when 25 then 'Cancelado'
        when 30 then 'Facturado'
        else 'Desconocido'
    end as `claim_status_name`,
    ct.claim_type_id,
    ct.claim_type_name, -- as `Tipo`,
    c.claim_id,
    c.external_order_number, -- as `No. AGRI`,
    c.claim_number, -- as `No. Siniestro`,
    c.policy_number, -- as `No. Poliza`,
    c.policy_subsection, -- as `Inciso`,
    c.report_number, -- as `No. Reporte`,
    c.items_quantity, -- as `Cristales`
    c.customer_full_name, -- as `?`
    c.date_created,
    case 
		when cs.date_created is null then null
        else cs.date_created
    end as signature_date_created
from `insurance_claims_db`.`claim` c
inner join `insurance_claims_db`.`claim_type` ct on c.claim_type_id = ct.claim_type_id
left join `insurance_claims_db`.`claim_signature` cs on c.claim_id = cs.claim_id
inner join `insurance_admin_db`.`insurance_company` i on c.insurance_company_id = i.insurance_company_id
inner join `insurance_admin_db`.`store` s on c.store_id = s.store_id;
