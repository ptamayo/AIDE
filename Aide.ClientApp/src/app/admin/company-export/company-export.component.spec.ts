import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CompanyExportComponent } from './company-export.component';

describe('CompanyExportComponent', () => {
  let component: CompanyExportComponent;
  let fixture: ComponentFixture<CompanyExportComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ CompanyExportComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CompanyExportComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
