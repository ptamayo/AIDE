import { ClaimStatusId } from "src/app/enums/claim-status-id.enum";

export class Dashboard1Filter {
    pageSize: number;
    pageNumber: number;
    keywords?: string;
    statusId?: ClaimStatusId;
    storeName?: string;
    serviceTypeId?: number;
    insuranceCompanyId?: number;
    startDate?: Date;
    endDate?: Date;
}
