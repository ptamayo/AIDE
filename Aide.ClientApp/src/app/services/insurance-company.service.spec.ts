import { TestBed } from '@angular/core/testing';

import { InsuranceCompanyService } from './insurance-company.service';

describe('InsuranceCompanyService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: InsuranceCompanyService = TestBed.get(InsuranceCompanyService);
    expect(service).toBeTruthy();
  });
});
