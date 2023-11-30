import { UserRoleId } from '../enums/user-role-id.enum';

export class NotificationFilter {
    pageSize: number;
    pageNumber: number;
    userId?: number;
    userEmail?: string;
    userRoleId?: UserRoleId;
    dateCreated?: string;
    dateLogout?: string;
    keywords?: string;
    unreadNotificationsOnly: boolean;
}
