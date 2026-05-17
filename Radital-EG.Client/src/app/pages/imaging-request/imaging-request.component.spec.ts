import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ImagingRequestComponent } from './imaging-request.component';

describe('ImagingRequestComponent', () => {
  let component: ImagingRequestComponent;
  let fixture: ComponentFixture<ImagingRequestComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ImagingRequestComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ImagingRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});