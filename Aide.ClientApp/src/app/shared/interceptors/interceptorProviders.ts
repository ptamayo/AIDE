import {HTTP_INTERCEPTORS} from '@angular/common/http';
import { TimezoneInterceptorService } from './timezone-interceptor.service';
import { TokenInterceptorService } from './token-interceptor.service';

export const interceptorProviders = [
    { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptorService, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: TimezoneInterceptorService, multi: true }
];