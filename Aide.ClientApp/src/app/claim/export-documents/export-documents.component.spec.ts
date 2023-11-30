import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ExportDocumentsComponent } from './export-documents.component';

describe('ExportDocumentsComponent', () => {
  let component: ExportDocumentsComponent;
  let fixture: ComponentFixture<ExportDocumentsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ExportDocumentsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ExportDocumentsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
