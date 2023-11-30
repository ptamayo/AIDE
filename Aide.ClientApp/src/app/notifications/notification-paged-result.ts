import { PagedResult } from '../models/paged-result';

export interface NotificationPagedResult extends PagedResult {
    totalUnreadNotifications: number;
}
