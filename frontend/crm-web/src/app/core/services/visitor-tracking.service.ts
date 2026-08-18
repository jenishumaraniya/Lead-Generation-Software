// User opens website
//        ↓
// Check localStorage
//        ↓
// Visitor ID exists?
//    ↓           ↓
//  YES          NO
//   ↓            ↓
// Continue    Call API
//                ↓
//          Get Anonymous ID
//                ↓
//          Save localStorage

import { Injectable } from '@angular/core';
import { ApiService } from './api.service';

export type CookieConsentChoice = 'accepted' | 'rejected';

@Injectable({
  providedIn: 'root'
})
export class VisitorTrackingService {

  private storageKey = 'anonymousVisitorId';
  private consentKey = 'crm_cookie_consent';

  constructor(private api: ApiService) {}

  getConsentChoice(): CookieConsentChoice | null {
    const cookieValue = this.getCookie(this.consentKey);

    if (cookieValue === 'accepted' || cookieValue === 'rejected') {
      return cookieValue;
    }

    const storedValue = localStorage.getItem(this.consentKey);

    if (storedValue === 'accepted' || storedValue === 'rejected') {
      return storedValue as CookieConsentChoice;
    }

    return null;
  }

  hasConsent(): boolean {
    return this.getConsentChoice() === 'accepted';
  }

  shouldShowConsentPopup(): boolean {
    return this.getConsentChoice() === null;
  }

  setConsentChoice(choice: CookieConsentChoice): void {
    this.setCookie(this.consentKey, choice, 365);
    localStorage.setItem(this.consentKey, choice);

    this.initializeVisitor(choice, () => {
      if (choice === 'accepted') {
        this.trackActivity('cookie_consent_accepted', undefined, {
          source: 'consent_popup',
          status: 'accepted'
        });
      }
    });
  }

  initializeVisitor(
    consentStatus?: CookieConsentChoice,
    onVisitorCreated?: () => void
  ): void {
    const existingVisitor = localStorage.getItem(this.storageKey);

    if (existingVisitor) {
      return;
    }

    const choice = consentStatus ?? this.getConsentChoice();

    if (!choice) {
      return;
    }

    this.api.createVisitor(choice).subscribe({
      next: (response) => {
        localStorage.setItem(
          this.storageKey,
          response.anonymousId
        );

        console.log(
          'Visitor created:',
          response.anonymousId,
          'Consent:',
          choice
        );

        onVisitorCreated?.();
      },

      error: (error) => {
        console.error(
          'Unable to create visitor',
          error
        );
      }
    });
  }

  getVisitorId(): string | null {
    return localStorage.getItem(
      this.storageKey
    );
  }

  trackActivity(
    activityType: string,
    productId?: number,
    metadata?: any
  ): void {
    if (!this.hasConsent()) {
      console.info(
        'Tracking blocked until consent is accepted.'
      );
      return;
    }

    const anonymousId =
      this.getVisitorId();

    if (!anonymousId) {
      console.warn(
        'Visitor ID not available'
      );

      return;
    }

    const activity = {
      anonymousId,
      activityType,
      productId: productId ?? null,
      pageUrl: window.location.pathname,
      metadata: metadata
        ? JSON.stringify(metadata)
        : null
    };

    this.api.recordActivity(activity)
      .subscribe({
        next: () => {
          console.log(
            'Activity tracked:',
            activityType
          );
        },

        error: (error) => {
          console.error(
            'Activity tracking failed',
            error
          );
        }
      });
  }

  private getCookie(key: string): string | null {
    const cookie = document.cookie
      .split('; ')
      .find((entry) => entry.startsWith(`${key}=`));

    return cookie ? decodeURIComponent(cookie.split('=').slice(1).join('=')) : null;
  }

  private setCookie(key: string, value: string, days: number): void {
    const expires = new Date();
    expires.setTime(expires.getTime() + (days * 24 * 60 * 60 * 1000));

    document.cookie = `${key}=${encodeURIComponent(value)}; expires=${expires.toUTCString()}; path=/; SameSite=Lax`;
  }
}