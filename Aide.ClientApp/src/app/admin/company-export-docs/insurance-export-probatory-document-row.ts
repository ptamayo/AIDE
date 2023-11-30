import { ExportDocumentTypeId } from "src/app/enums/export-document-type-id.enum";
import { ExportTypeId } from "src/app/enums/export-type-id";

export interface InsuranceExportProbatoryDocumentRow {
    name: string;
    exportDocumentTypeId: ExportDocumentTypeId;
    probatoryDocumentId?: number;
    collageId?: number;
    exportTypeId: ExportTypeId;
}