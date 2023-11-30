import { CompanyClaimTypeSettings } from "./company-claim-type-settings";

export interface Company {
    id: number;
    name: string;
    isEnabled: boolean;

    claimTypeSettings?: { [claimId: number]: CompanyClaimTypeSettings } | {};
}
