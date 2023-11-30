import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ClaimDocumentsComponent } from './claim-documents.component';

describe('ClaimDocumentsComponent', () => {
  let component: ClaimDocumentsComponent;
  let fixture: ComponentFixture<ClaimDocumentsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ClaimDocumentsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ClaimDocumentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
