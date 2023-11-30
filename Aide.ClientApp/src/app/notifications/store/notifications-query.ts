import { Injectable } from '@angular/core';
import { QueryEntity } from '@datorama/akita';
import { NotificationState } from './notification-state';
import { Notification } from '../../notifications/notification';
import { NotificationsStore } from './notifications-store';

@Injectable({
    providedIn: 'root'
})
export class NotificationsQuery extends QueryEntity<NotificationState, Notification> {
    constructor(protected store: NotificationsStore) {
        super(store);
    }
}
