import { UserRoleId } from 'src/app/enums/user-role-id.enum';

export class UsersFilter {
    pageSize: number;
    pageNumber: number;
    keywords?: string;
    companyId?: number;
    companyTypeId?: number;
    userRoleIds?: UserRoleId[] | [];
}
