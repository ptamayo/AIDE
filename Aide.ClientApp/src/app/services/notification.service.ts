import { Injectable } from '@angular/core';
import { DataService } from '../shared/services/data.service';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Observable } from 'rxjs';
import { NotificationFilter } from '../notifications/notification.filter';
import { ReadNotificationRequest } from '../notifications/read-notification-request';
import { NotificationsStore } from '../notifications/store/notifications-store';
import { Notification } from '../notifications/notification';
import { NotificationPagedResult } from '../notifications/notification-paged-result';
import { map, catchError } from 'rxjs/operators';

@Injectable()
export class NotificationService extends DataService {

  constructor(http: HttpClient, private notificationsStore: NotificationsStore) { 
    super(environment.endpointNotificationService, http);
  }

  getAll(pagingAndFilters: NotificationFilter): Observable<Object> {
    return this.post(pagingAndFilters);
  }

  postRead(request: ReadNotificationRequest) {
    return this.post(request, '/read');
  }

  populateStore(pagingAndFilters: NotificationFilter) {
    this.getAll(pagingAndFilters)
      .pipe(
        map((page: NotificationPagedResult) => {
          return page.results as Notification[];
        }),
        catchError(() => {
          return [];
        })
      )
      .subscribe((data: Notification[]) => {
        this.notificationsStore.set(data);
      });
  }

  upsertStore(notification: Notification) {
    this.notificationsStore.upsert(notification.id, notification);
  }

  complete(request: ReadNotificationRequest): Observable<Object> {
    request.notificationId.forEach(id => this.notificationsStore.upsert(id, { isRead: true }));
    return this.postRead(request);
  }
}
