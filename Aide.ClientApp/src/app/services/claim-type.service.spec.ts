import { TestBed } from '@angular/core/testing';

import { ClaimTypeService } from './claim-type.service';

describe('ClaimTypeService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ClaimTypeService = TestBed.get(ClaimTypeService);
    expect(service).toBeTruthy();
  });
});
