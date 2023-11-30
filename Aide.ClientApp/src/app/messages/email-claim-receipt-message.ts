export interface EmailClaimReceiptMessage {
    claimId: number;
    claimNumber: string;
    externalOrderNumber: string;
    customerFullName: string;
    insuranceCompanyName: string;
    storeName: string;
    storeEmail: string;
    claimProbatoryDocuments: string[];
}
