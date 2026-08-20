import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FloatingContactButtonComponent } from './floating-contact-button.component';

describe('FloatingContactButtonComponent', () => {
  let component: FloatingContactButtonComponent;
  let fixture: ComponentFixture<FloatingContactButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FloatingContactButtonComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(FloatingContactButtonComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
