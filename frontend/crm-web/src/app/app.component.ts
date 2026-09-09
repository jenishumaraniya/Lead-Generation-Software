import { Component, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { Router, RouterLink, RouterOutlet, NavigationStart, NavigationEnd, NavigationCancel, NavigationError } from '@angular/router';
import { filter } from 'rxjs/operators';
import { VisitorTrackingService } from './core/services/visitor-tracking.service';
import { ContactService } from './core/services/contact.service';
import { FloatingContactButtonComponent } from './components/floating-contact-button/floating-contact-button.component';
import { ContactFormComponent, ContactFormData } from './components/contact-form/contact-form.component';
import { ToastContainerComponent } from './components/toast-container/toast-container.component';
import { ConfirmDialogComponent } from './components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NgIf, RouterOutlet, FloatingContactButtonComponent, RouterLink, ContactFormComponent, ToastContainerComponent, ConfirmDialogComponent],
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

    this.router.events.subscribe(event => {
      if (event instanceof NavigationStart) {
        this.isLoading = true;
      } else if (
        event instanceof NavigationEnd ||
        event instanceof NavigationCancel ||
        event instanceof NavigationError
      ) {
        this.isLoading = false;
        if (event instanceof NavigationEnd) {
          updateRouteStatus(event.urlAfterRedirects || this.router.url);
        }
      }
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

    this.ensureFavicon();
  }

  private ensureFavicon(): void {
    if (typeof document === 'undefined') return;
    try {
      const b64 = 'data:image/png;base64,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';
      const existingIcons = document.querySelectorAll("link[rel*='icon']");
      if (existingIcons.length > 0) {
        existingIcons.forEach(el => (el as HTMLLinkElement).href = b64);
      } else {
        const link = document.createElement('link');
        link.rel = 'icon';
        link.type = 'image/png';
        link.href = b64;
        document.head.appendChild(link);
      }
    } catch (_) {}
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