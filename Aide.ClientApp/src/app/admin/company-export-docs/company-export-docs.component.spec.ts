import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CompanyExportDocsComponent } from './company-export-docs.component';

describe('CompanyExportDocsComponent', () => {
  let component: CompanyExportDocsComponent;
  let fixture: ComponentFixture<CompanyExportDocsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CompanyExportDocsComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CompanyExportDocsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
