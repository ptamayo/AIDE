import { TestBed } from '@angular/core/testing';

import { ClaimProbatoryDocumentsService } from './claim-probatory-documents.service';

describe('ClaimProbatoryDocumentsService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ClaimProbatoryDocumentsService = TestBed.get(ClaimProbatoryDocumentsService);
    expect(service).toBeTruthy();
  });
});
