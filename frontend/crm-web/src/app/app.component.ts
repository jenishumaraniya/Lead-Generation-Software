import { NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { VisitorTrackingService } from './core/services/visitor-tracking.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [NgIf, RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'crm-web';
  showConsentPopup = false;

  constructor(private visitorTrackingService: VisitorTrackingService) {}

  ngOnInit(): void {
    this.showConsentPopup = this.visitorTrackingService.shouldShowConsentPopup();

    if (this.visitorTrackingService.hasConsent()) {
      this.visitorTrackingService.initializeVisitor(
        this.visitorTrackingService.getConsentChoice() ?? 'accepted'
      );
      this.visitorTrackingService.trackActivity('page_view');
    }
  }

  acceptCookies(): void {
    this.visitorTrackingService.setConsentChoice('accepted');
    this.showConsentPopup = false;
  }

  rejectCookies(): void {
    this.visitorTrackingService.setConsentChoice('rejected');
    this.showConsentPopup = false;
  }
}
