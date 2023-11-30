USE `db_aide_claims`;

INSERT
INTO claim_status
	(claim_status_id
    ,claim_status_name)
VALUES
(10, 'En proceso'),
(20, 'Completado'),
(25, 'Cancelado'),
(30, 'Facturado');

INSERT
INTO claim_type
	(claim_type_id
	,claim_type_name
	,sort_priority
	,date_created
	,date_modified)
VALUES
	(1, 'Siniestro de atención en sucursal', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
	(2, 'Orden de servicio', 2, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
	(3, 'Vale de colisión', 3, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
	(4, 'Desmonta y monta', 4, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
	(5, 'Reparación de parabrisas', 5, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
	(6, 'Calibración de parabrisas', 6, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
	(7, 'Verificación vehicular', 7, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT
INTO document_type
	(document_type_id,
    document_type_name,
    date_created, 
    date_modified)
VALUES
	(100, 'PDF Factura Electrónica', UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (101, 'XML Factura Electrónica', UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (102, 'Archivo ZIP con Documentos Probatorios', UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (103, 'Recibo firmado por asegurado', UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (104, 'Ficha de depósito del deducible', UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (105, 'Archivo PDF con Documentos Probatorios', UTC_TIMESTAMP(),  UTC_TIMESTAMP());

INSERT
INTO claim_document_type
	(claim_document_type_id,
    document_type_id,
    sort_priority,
    group_id,
    date_created, 
    date_modified)
VALUES
	(1, 100, 1, 1, UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (2, 101, 2, 1, UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (3, 102, 1, 2, UTC_TIMESTAMP(),  UTC_TIMESTAMP()),
    (4, 103, 1, 3, UTC_TIMESTAMP(),  UTC_TIMESTAMP());
