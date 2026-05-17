import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TechnicianDashboard } from './technician-dashboard';

describe('TechnicianDashboard', () => {
  let component: TechnicianDashboard;
  let fixture: ComponentFixture<TechnicianDashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TechnicianDashboard]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TechnicianDashboard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
