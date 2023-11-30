import { TestBed } from '@angular/core/testing';

import { TimezoneInterceptorService } from './timezone-interceptor.service';

describe('TimezoneInterceptorService', () => {
  let service: TimezoneInterceptorService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TimezoneInterceptorService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
