import { EntityState } from '@datorama/akita';
import { Notification } from '../../notifications/notification';

export interface NotificationState extends EntityState<Notification> {
}
