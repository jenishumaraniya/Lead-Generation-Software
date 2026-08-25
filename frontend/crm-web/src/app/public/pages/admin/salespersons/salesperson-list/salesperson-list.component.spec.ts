import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SalespersonListComponent } from './salesperson-list.component';

describe('SalespersonListComponent', () => {
  let component: SalespersonListComponent;
  let fixture: ComponentFixture<SalespersonListComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SalespersonListComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(SalespersonListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
