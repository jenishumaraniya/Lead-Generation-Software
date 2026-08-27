import { Component, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { Router, RouterLink, RouterOutlet, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs/operators';
import { VisitorTrackingService } from './core/services/visitor-tracking.service';
import { ContactService } from './core/services/contact.service';
import { FloatingContactButtonComponent } from './components/floating-contact-button/floating-contact-button.component';
import { ContactFormComponent, ContactFormData } from './components/contact-form/contact-form.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NgIf, RouterOutlet, FloatingContactButtonComponent, RouterLink, ContactFormComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'crm-web';
  showConsentPopup = false;
  showContactForm = false;
  preselectedProductId?: number;
  isLoading = false;
  isPublicRoute = true;

  toastMessage: string = '';
  isToastError: boolean = false;

  constructor(
    private visitorTrackingService: VisitorTrackingService,
    public router: Router,
    private contactService: ContactService
  ) {}

  ngOnInit(): void {
    const updateRouteStatus = (url: string) => {
      this.isPublicRoute = !url.startsWith('/admin') && !url.startsWith('/sales') && !url.startsWith('/login');
    };

    updateRouteStatus(this.router.url);

    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      updateRouteStatus(event.urlAfterRedirects || this.router.url);
    });

    this.showConsentPopup = this.visitorTrackingService.shouldShowConsentPopup();

    if (this.visitorTrackingService.hasConsent()) {
      this.visitorTrackingService.initializeVisitor(
        this.visitorTrackingService.getConsentChoice() ?? 'accepted'
      );
      this.visitorTrackingService.trackActivity('PAGE_VIEW');
    }

    this.contactService.openForm$.subscribe(productId => {
      this.openContactForm(productId);
    });
  }

  isActiveRoute(path: string): boolean {
    return this.router.url === path;
  }

  acceptCookies(): void {
    this.visitorTrackingService.setConsentChoice('accepted');
    this.showConsentPopup = false;
  }

  rejectCookies(): void {
    this.visitorTrackingService.setConsentChoice('rejected');
    this.showConsentPopup = false;
  }

  scrollToContact(): void {
    this.openContactForm();
    if (this.visitorTrackingService.hasConsent()) {
      this.visitorTrackingService.trackActivity(
        'INTEREST_CLICK',
        undefined,
        { source: 'navbar_contact' }
      );
    }
  }

  openContactForm(productId?: number): void {
    this.preselectedProductId = productId;
    this.showContactForm = true;
   
    if (this.visitorTrackingService.hasConsent()) {
      if (productId) {
        this.visitorTrackingService.trackActivity(
          'INTEREST_CLICK',
          productId,
          { source: 'floating_button' }
        );
      } else {
        this.visitorTrackingService.trackActivity(
          'INTEREST_CLICK',
          undefined,
          { source: 'contact_us_button' }
        );
      }
    }
  }

  closeContactForm(): void {
    this.showContactForm = false;
    this.preselectedProductId = undefined;
  }

  handleFormSubmit(formData: ContactFormData): void {
    console.log('Form submitted:', formData);
  }

  handleLeadSubmitted(response: any): void {
    console.log('Lead created:', response);
  }

  showToastMessage(message: string): void {
    this.toastMessage = message;
    this.isToastError = true;
    setTimeout(() => {
      this.toastMessage = '';
    }, 5000);
  }
}