import { TestBed } from '@angular/core/testing';

import { ClaimReceiptService } from './claim-receipt.service';

describe('ClaimReceiptService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ClaimReceiptService = TestBed.get(ClaimReceiptService);
    expect(service).toBeTruthy();
  });
});
