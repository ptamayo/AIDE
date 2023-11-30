import { ProbatoryDocument } from './probatory-document';
import { ClaimTypeId } from '../enums/claim-type-id.enum';

export class InsuranceProbatoryDocument {
    id: number;
    insuranceCompanyId: number;
    claimTypeId: ClaimTypeId;
    probatoryDocumentId: number;
    sortPriority: number;
    groupId: number;
    dateCreated?: string;
    dateModified?: string;
    probatoryDocument?: ProbatoryDocument;
}
