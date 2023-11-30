import { Injectable } from '@angular/core';
import * as signalR from "@aspnet/signalr";
import { environment } from 'src/environments/environment';
import { Subject, Observable } from 'rxjs';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  private hubConnection: signalR.HubConnection;
  private notificationReceivedSubject = new Subject<any>();

  constructor(private authService: AuthService) { 
    // this.buildConnection();
    // this.startConnection();
  }

  public buildConnection = () => {
    if (!this.hubConnection) {
      this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.endpointNotificationHub}?token=${this.authService.token}`)
      .withAutomaticReconnect([0, 2000, 10000, 15000, 30000, 45000, 60000])
      .configureLogging(signalR.LogLevel.Information)
      .build();
    }
  }

  public startConnection = () => {
    if (this.hubConnection) {
      this.hubConnection
      .start()
      .then(() => {
        console.log("Connection started! ...");
        this.registerServiceEvents();
      })
      .catch(err => {
        console.log("Error while starting connection: " + err);
        setTimeout(() => {
          // if error then retry again after 5 secs
          this.startConnection();
        }, 5000);
      });
    }
  }

  private registerServiceEvents() {
    // this.hubConnection.on("Broadcast", (notitication) => {
    //   this.catchNotification(notitication);
    // });
    this.hubConnection.on("PrivateMessage", (notitication) => {
      this.catchNotification(notitication);
    });
    this.hubConnection.on("GroupMessage", (notitication) => {
      this.catchNotification(notitication);
    });
  }

  // Receives notification from signalR hub and enqueues/send to a local subject
  private catchNotification(notification: any){
    this.notificationReceivedSubject.next(notification);
  }

  // Commented out becasue not use case has been identified yet
  // public clearNotifications() {
  //   this.notificationReceivedSubject.next();
  // }

  public stopConnection(){
    if (this.hubConnection) {
      this.hubConnection.stop();
      console.log("Connection closed! ...");
      this.hubConnection = null;
    }
  }

  // Second alternative of event
  // public onNotificationReceived(): Observable<any> {
  //   return this.notificationReceivedSubject.asObservable();
  // }

  get onNotificationReceived$(): Observable<any> {
    return this.notificationReceivedSubject.asObservable();
  }
}
