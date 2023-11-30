import { Injectable } from '@angular/core';
import { EntityStore, StoreConfig } from '@datorama/akita';
import { Notification } from '../../notifications/notification';
import { NotificationState } from './notification-state';

@Injectable({ providedIn: 'root' })
@StoreConfig({ name: 'notifications' })
export class NotificationsStore extends EntityStore<NotificationState, Notification> {
    constructor() {
        super({ });
    }
}
