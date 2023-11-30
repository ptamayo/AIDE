import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ClaimProbatoryDocumentsComponent } from './claim-probatory-documents.component';

describe('ClaimProbatoryDocumentsComponent', () => {
  let component: ClaimProbatoryDocumentsComponent;
  let fixture: ComponentFixture<ClaimProbatoryDocumentsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ClaimProbatoryDocumentsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ClaimProbatoryDocumentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
