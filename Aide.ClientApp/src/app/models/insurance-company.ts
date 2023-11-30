import { CompanyClaimTypeSettings } from "./company-claim-type-settings";

export interface InsuranceCompany {
    id: number;
    name: string;
    isEnabled: boolean;
    dateCreated: string;
    dateModified: string;

    claimTypeSettings?: { [claimId: number]: CompanyClaimTypeSettings } | {};
}
