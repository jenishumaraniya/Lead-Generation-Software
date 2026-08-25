import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LeadListComponent } from './lead-list.component';

describe('LeadListComponent', () => {
  let component: LeadListComponent;
  let fixture: ComponentFixture<LeadListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LeadListComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(LeadListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
