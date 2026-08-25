import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SalesLeadListComponent } from './sales-lead-list.component';

describe('SalesLeadListComponent', () => {
  let component: SalesLeadListComponent;
  let fixture: ComponentFixture<SalesLeadListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SalesLeadListComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SalesLeadListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
