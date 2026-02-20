import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminErrorLogsComponent } from './admin-error-logs.component';

describe('AdminErrorLogsComponent', () => {
  let component: AdminErrorLogsComponent;
  let fixture: ComponentFixture<AdminErrorLogsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminErrorLogsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AdminErrorLogsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
