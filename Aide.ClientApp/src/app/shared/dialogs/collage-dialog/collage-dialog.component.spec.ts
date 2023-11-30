import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { CollageDialogComponent } from './collage-dialog.component';

describe('CollageDialogComponent', () => {
  let component: CollageDialogComponent;
  let fixture: ComponentFixture<CollageDialogComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ CollageDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CollageDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
