import { Component, OnInit } from '@angular/core';
import { NgIf } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { VisitorTrackingService } from './core/services/visitor-tracking.service';
import { ContactService } from './core/services/contact.service';
import { FloatingContactButtonComponent } from './components/floating-contact-button/floating-contact-button.component';
import { ContactFormComponent, ContactFormData } from './components/contact-form/contact-form.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NgIf, RouterOutlet, FloatingContactButtonComponent, ContactFormComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'crm-web';
  showConsentPopup = false;
  showContactForm = false;
  preselectedProductId?: number;

  constructor(
    private visitorTrackingService: VisitorTrackingService,
    private contactService: ContactService
  ) {}

  ngOnInit(): void {
    this.showConsentPopup = this.visitorTrackingService.shouldShowConsentPopup();

    if (this.visitorTrackingService.hasConsent()) {
      this.visitorTrackingService.initializeVisitor(
        this.visitorTrackingService.getConsentChoice() ?? 'accepted'
      );
      this.visitorTrackingService.trackActivity('PAGE_VIEW');
    }

    // Listen for contact form open requests
    this.contactService.openForm$.subscribe(productId => {
      this.openContactForm(productId);
    });
  }

  acceptCookies(): void {
    this.visitorTrackingService.setConsentChoice('accepted');
    this.showConsentPopup = false;
  }

  rejectCookies(): void {
    this.visitorTrackingService.setConsentChoice('rejected');
    this.showConsentPopup = false;
  }

  // Contact Form Methods
  openContactForm(productId?: number): void {
    this.preselectedProductId = productId;
    this.showContactForm = true;
    
    // Track the event
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

  closeContactForm(): void {
    this.showContactForm = false;
    this.preselectedProductId = undefined;
  }

  handleFormSubmit(formData: ContactFormData): void {
    console.log('Form submitted:', formData);
    // TODO: Send to your API
    
    // Track the submission
    this.visitorTrackingService.trackActivity(
      'INTEREST_CLICK',
      formData.products[0],
      { 
        source: 'contact_form_submission',
        products: formData.products,
        timeline: formData.timeline
      }
    );
    
    alert('Thank you for your interest! Our team will contact you soon.');
  }
}