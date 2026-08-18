import { ComponentFixture, TestBed } from '@angular/core/testing';

import { VisitorDetailsComponent } from './visitor-details.component';

describe('VisitorDetailsComponent', () => {
  let component: VisitorDetailsComponent;
  let fixture: ComponentFixture<VisitorDetailsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [VisitorDetailsComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(VisitorDetailsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
