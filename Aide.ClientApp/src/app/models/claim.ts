import { ClaimType } from './claim-type';
import { ClaimProbatoryDocument } from './claim-probatory-document';
import { ClaimCreatedByUser } from './claim-created-by-user';
import { ClaimStore } from './claim-store';
import { ClaimInsuranceCompany } from './claim-insurance-company';
import { ClaimDocument } from './claim-document';

export class Claim {
    id: number;
    claimStatusId: number;
    claimTypeId: number;
    customerFullName: string;
    policyNumber: string;
    policySubsection: string;
    claimNumber: string;
    reportNumber: string;
    externalOrderNumber: string;
    insuranceCompanyId: number;
    storeId: number;
    claimProbatoryDocumentStatusId: number;
    isDepositSlipRequired: boolean;
    hasDepositSlip: boolean;
    itemsQuantity: number;
    createdByUserId: number;
    dateCreated?: string;
    dateModified?: string;
    source?: string;

    createdByUser?: ClaimCreatedByUser | null;
    claimType?: ClaimType | null;
    store?: ClaimStore | null;
    insuranceCompany?: ClaimInsuranceCompany | null;
    claimDocuments?: ClaimDocument[] | [];
    claimProbatoryDocuments?: ClaimProbatoryDocument[];
}
