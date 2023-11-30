import { UserRoleId } from '../enums/user-role-id.enum';
import { UserCompany } from './user-company';

export interface User {
    id: number;
    roleId: UserRoleId;
    firstName: string;
    lastName: string;
    email: string;
    companies: UserCompany[];
    dateCreated?: string;
    dateModified?: string;
    dateLogout?: string;
}
