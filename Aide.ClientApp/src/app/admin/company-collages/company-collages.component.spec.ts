import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { CompanyCollagesComponent } from './company-collages.component';

describe('CompanyCollagesComponent', () => {
  let component: CompanyCollagesComponent;
  let fixture: ComponentFixture<CompanyCollagesComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ CompanyCollagesComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CompanyCollagesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
