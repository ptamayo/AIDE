import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { CompanyServiceDocsComponent } from './company-service-docs.component';

describe('CompanyServiceDocsComponent', () => {
  let component: CompanyServiceDocsComponent;
  let fixture: ComponentFixture<CompanyServiceDocsComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ CompanyServiceDocsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CompanyServiceDocsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
