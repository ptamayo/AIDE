import { ClaimType } from './claim-type';
import { InsuranceCollageProbatoryDocument } from './insurance-collage-probatory-document';

export class InsuranceCollage {
    id: number;
    name: string;
    insuranceCompanyId: number;
    claimType?: ClaimType;
    claimTypeId: number;
    columns: number;
    dateCreated?: string;
    dateModified?: string;

    probatoryDocuments?: InsuranceCollageProbatoryDocument[] | [];
}
