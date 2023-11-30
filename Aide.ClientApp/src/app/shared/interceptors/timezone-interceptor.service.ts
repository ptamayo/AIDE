import { HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable()
export class TimezoneInterceptorService implements HttpInterceptor {

  constructor() { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Retrieve the local timezone
    const timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    // If any, then append it to new headers
    let newHeaders = req.headers;
    if (timezone) {
      newHeaders = newHeaders.append('Timezone', `${timezone}`);
    }
    // Clone the request with the new headers
    const authReq = req.clone({ headers: newHeaders });
    // Finally return an observable
    return next.handle(authReq);
  }
}
