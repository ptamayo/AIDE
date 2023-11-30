import { ClaimTypeId } from '../enums/claim-type-id.enum';
import { ExportDocumentTypeId } from '../enums/export-document-type-id.enum';
import { ExportTypeId } from '../enums/export-type-id';

export class InsuranceExportProbatoryDocument {
    exportTypeId: ExportTypeId;
    insuranceCompanyId: number;
    claimTypeId: ClaimTypeId;
    exportDocumentTypeId: ExportDocumentTypeId;
    sortPriority: number;
    probatoryDocumentId?: number;
    collageId?: number;
    name: string;
    dateCreated?: string;
    dateModified?: string;
}
