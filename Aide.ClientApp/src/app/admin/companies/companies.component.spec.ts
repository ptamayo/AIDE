import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { CompaniesComponent } from './companies.component';

describe('CompaniesComponent', () => {
  let component: CompaniesComponent;
  let fixture: ComponentFixture<CompaniesComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ CompaniesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CompaniesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
