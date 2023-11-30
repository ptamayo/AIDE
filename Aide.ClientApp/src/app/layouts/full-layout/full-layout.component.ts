import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Subject, Subscription } from 'rxjs';
import { ResizeService } from 'src/app/shared/services/resize.service';
import { delay } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { AuthService } from 'src/app/services/auth.service';
import { Router } from '@angular/router';
import { UserRoleId } from 'src/app/enums/user-role-id.enum';
import { NotificationService } from 'src/app/services/notification.service';
import { SignalRService } from 'src/app/services/signal-r.service';
import { Notification } from 'src/app/notifications/notification';
import { NotificationFilter } from 'src/app/notifications/notification.filter';
import { ReadNotificationRequest } from 'src/app/notifications/read-notification-request';
import { MatSidenav } from '@angular/material/sidenav';
import { NotificationsQuery } from 'src/app/notifications/store/notifications-query';
import { MediaService } from 'src/app/services/media.service';
import { OpenMediaRequest } from 'src/app/models/open-media-request';
import { OpenUrlRequest } from 'src/app/models/open-url-request';
import { OpenBlob } from 'src/app/shared/helpers/open-blob';

const HslColorSaturation = '80%';
const HslColorLightness = '70%';

@Component({
  selector: 'app-full-layout',
  templateUrl: './full-layout.component.html',
  styleUrls: ['./full-layout.component.css']
})
export class FullLayoutComponent implements OnInit, OnDestroy {
  @ViewChild('sidenavm') sidenavMenu: MatSidenav;
  @ViewChild('sidenav') sidenavNotifications: MatSidenav;
  private openDocumentSubject: Subject<OpenMediaRequest> = new Subject<OpenMediaRequest>();
  private screenResizeSubscription: Subscription;
  private notificationStoreSubscription: Subscription;
  private notificationSubscription: Subscription;
  sidenavOpened: boolean = false;
  sidenavNotificationsOpened: boolean;
  isMobile: boolean;
  isPhone: boolean; // This is the smallest size of a mobile device
  notifications: Notification[] = [];
  unreadNotificationsCount: number = 0;

  urlLogo(): string {
    return environment.logo;
  }
  
  get userRole() {
    return UserRoleId;
  }

  get userInitials() {
    return (this.authService.currentUser.firstName.length > 0 ? this.authService.currentUser.firstName[0] : '') +
          (this.authService.currentUser.lastName.length > 0 ? this.authService.currentUser.lastName[0] : '');
  }

  get stringToHslColor() {
    const initials = this.userInitials;
    let hash = 0;
    for (var i = 0; i < initials.length; i++) {
      hash = initials.charCodeAt(i) + ((hash << 5) - hash);
    }
    const h = hash % 360;
    const hslColor = 'hsl('+h+', '+HslColorSaturation+', '+HslColorLightness+')';
    return hslColor;
  }

  get displayMenuDescription(): string {
    return this.sidenavOpened ? 'none' : 'block';
  }

  constructor(
    public authService: AuthService, 
    private router: Router, 
    private resizeSvc: ResizeService,
    private notificationService: NotificationService,
    private notificationsQuery: NotificationsQuery,
    private signalRService: SignalRService,
    private mediaService: MediaService) {
    // Screen resize subscription
    this.screenResizeSubscription = this.resizeSvc.onResize$.pipe(delay(0)).subscribe(() => this.showOrHideElementsOnTemplate());
    this.showOrHideElementsOnTemplate();
    // Notifications store
    this.notificationStoreSubscription = this.notificationsQuery.selectAll().subscribe(data => {
      // Calculate the unread notifications count
      this.unreadNotificationsCount = data.filter(x => x.isRead === false).length;
      // Refresh the list of notifications
      this.notifications = data.sort((a, b) => (a.id < b.id) ? 1 : -1); // Need revisit: Order by notification id descending
    });
    // SignalR subscription
    this.notificationSubscription = this.signalRService.onNotificationReceived$.subscribe((notification: Notification) => {
      if (notification) {
        // Increase the count of unread messages
        this.unreadNotificationsCount++;
        // Insert new notification at the beginning of the array
        // this.notifications.unshift(notification);
        // Upsert the notification into the store
        this.notificationService.upsertStore(notification);
      }
    });
  }

  ngOnInit() {
    // User Notifications
    const pagingAndFilters: NotificationFilter = {
      pageSize: environment.notificationsPageSize,
      pageNumber: 1,
      userId: this.authService.currentUser.id,
      userEmail: this.authService.currentUser.email,
      userRoleId: this.authService.currentUser.roleId,
      dateCreated: this.authService.currentUser.dateCreated,
      dateLogout: this.authService.currentUser.dateLogout,
      unreadNotificationsOnly: true
    };
    // Populate the notifications store from database
    this.notificationService.populateStore(pagingAndFilters);
    // SignalR subscription
    this.signalRService.buildConnection();
    this.signalRService.startConnection();

    this.openDocumentSubject.subscribe(request => OpenBlob(request));
  }

  logout() {
    this.signalRService.stopConnection();
    this.authService.logout({ userId: this.authService.currentUser.id }).subscribe();
    this.router.navigate(['/login']);
  }

  showOrHideElementsOnTemplate() {
    if (environment.screenSize < 3) {
      this.sidenavOpened = false;
      this.isMobile = true;
      if (environment.screenSize == 0) {
        this.isPhone = true;
      }
      else {
        this.isPhone = false;
      }
    }
    else {
      this.sidenavOpened = true;
      this.isMobile = false;
      this.isPhone = false;
    }
  }

  jsonParse(n: Notification) {
    return Notification.jsonParse(n);
  }

  download(notification: Notification) {
    // if (this.isDownloading[index]) return;
    // this.isDownloading[index] = true;
    if (this.jsonParse(notification)?.documentId) {
      this.mediaService.downloadDocument(this.jsonParse(notification)?.documentId).subscribe(blob => {
        const request = <OpenMediaRequest> {
          name: this.jsonParse(notification).title,
          blob: blob
        };
        this.openDocumentSubject.next(request);
        // this.isDownloading[index] = false;
      },
      error => {
          console.log(error?.message);
          // this.openSnackBar(`Cannot download the file of ${this.jsonParse(notification)?.title}`, 'Dismiss'); // ***
          // this.isDownloading[index] = false;
      });
      return;
    }

    if (this.jsonParse(notification)?.mediaId) {
      this.mediaService.downloadMedia(this.jsonParse(notification)?.mediaId).subscribe(blob => {
        const request = <OpenMediaRequest> {
          name: this.jsonParse(notification).title,
          blob: blob
        };
        this.openDocumentSubject.next(request);
        // this.isDownloading[index] = false;
      },
      error => {
          console.log(error?.message);
          // this.openSnackBar(`Cannot download the file of ${this.jsonParse(notification)?.title}`, 'Dismiss'); // ***
          // this.isDownloading[index] = false;
      });
      return;
    }

    if (!this.jsonParse(notification)?.documentId && !this.jsonParse(notification)?.mediaId) {
      const request = <OpenUrlRequest> {
        name: this.jsonParse(notification).title,
        url: this.jsonParse(notification).url
      };
      this.openUrl(request);
      return;
    }
  }

  openUrl(request: OpenUrlRequest) {
    var downloadURL = request.url;
    var link = document.createElement('a');
    link.href = downloadURL;
    // link.target = "_blank"; // If you wanna open the file in a separated tab
    const fileExtension = request.url.split('.')[request.url.split('.').length-1];
    link.download = `${request.name}.${fileExtension}`;
    link.click();
    window.URL.revokeObjectURL(downloadURL);
  }

  redirectToFromMenu(url) {
    this.router.navigateByUrl('/', {skipLocationChange: true}).then(() =>
    this.router.navigate(url));
    if (this.isMobile || this.isPhone) {
      this.sidenavMenu.close();
    }
    if (this.sidenavNotificationsOpened) {
      this.sidenavNotifications.close();
    }
  }

  redirectToFromMenu2(url) {
    this.router.navigateByUrl('/', {skipLocationChange: false}).then(() =>
    this.router.navigate(url));
    if (this.isMobile || this.isPhone) {
      this.sidenavMenu.close();
    }
    if (this.sidenavNotificationsOpened) {
      this.sidenavNotifications.close();
    }
  }

  redirectTo(url) {
    this.router.navigateByUrl('/', {skipLocationChange: true}).then(() =>
    this.router.navigate(url));
  }

  // This method is functional.
  // It was commented out due to simplification in the logic.
  // markNotificationAsRead(notificationId) {
  //   const request: ReadNotificationRequest = {
  //     userId: this.authService.currentUser.id,
  //     notificationId: notificationId
  //   };
  //   this.notificationService.postRead(request).subscribe(data => {
  //     this.unreadNotificationsCount--;
  //   });
  // }

  setAllNotifictionsToReadStatus() {
    this.sidenavNotificationsOpened = !this.sidenavNotificationsOpened;
    if (!this.sidenavNotificationsOpened && this.unreadNotificationsCount > 0) {
      const unreadNotificationIds = this.notifications.filter(f => !f.isRead).map(m => m.id);
      this.unreadNotificationsCount -= unreadNotificationIds.length;
      const request: ReadNotificationRequest = {
        userId: this.authService.currentUser.id,
        notificationId: unreadNotificationIds
      };
      this.notificationService.complete(request).subscribe();
    }
  }

  closeSidenavNotifications() {
    if (this.sidenavNotificationsOpened) {
      this.sidenavNotifications.close();
      this.setAllNotifictionsToReadStatus();
    }
    else {
      this.sidenavNotifications.toggle();
    }
  }

  ngOnDestroy() {
    // unsubscribe to ensure no memory leaks
    this.screenResizeSubscription.unsubscribe();
    this.notificationStoreSubscription.unsubscribe();
    this.signalRService.stopConnection();
    this.notificationSubscription.unsubscribe();
  }
}
