import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppComponent],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should display the consent popup when no decision has been stored', () => {
    localStorage.clear();
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.cookie-consent')?.textContent).toContain('Accept');
    expect(compiled.querySelector('.cookie-consent')?.textContent).toContain('Reject');
  });

  it('should store a consent choice when the user accepts cookies', () => {
    localStorage.clear();
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('[data-testid="accept-cookie-consent"]') as HTMLButtonElement;
    button.click();

    expect(localStorage.getItem('crm_cookie_consent')).toBe('accepted');
  });
});
