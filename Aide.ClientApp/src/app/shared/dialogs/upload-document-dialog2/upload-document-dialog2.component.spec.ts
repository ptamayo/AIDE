import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { UploadDocumentDialog2Component } from './upload-document-dialog2.component';

describe('UploadDocumentDialog2Component', () => {
  let component: UploadDocumentDialog2Component;
  let fixture: ComponentFixture<UploadDocumentDialog2Component>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ UploadDocumentDialog2Component ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(UploadDocumentDialog2Component);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
