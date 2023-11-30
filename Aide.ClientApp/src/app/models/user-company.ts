import { CompanyTypeId } from '../enums/company-type-id.enum';

export interface UserCompany {
    id: number;
    // userId: number;
    companyId: number;
    companyTypeId: CompanyTypeId;
    // dateCreated: string;
}
