import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from 'src/app/services/auth.service';

@Injectable()
export class TokenInterceptorService implements HttpInterceptor {

  constructor(private authService: AuthService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Retrieve the token
    const token = this.authService.token;
    // If any, then append it to new headers
    let newHeaders = req.headers;
    if (token) {
      newHeaders = newHeaders.append('Authorization', `Bearer ${token}`);
      // newHeaders = newHeaders.append('Content-Type', 'application/json'); // To Do: Create an HttpInterceptor and remove the Content-Type header on data.service.ts
    }
    // Clone the request with the new headers
    const authReq = req.clone({ headers: newHeaders });
    // Finally return an observable
    return next.handle(authReq);
  }
}
