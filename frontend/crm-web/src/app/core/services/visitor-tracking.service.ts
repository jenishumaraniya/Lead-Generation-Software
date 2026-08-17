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

@Injectable({
  providedIn: 'root'
})
export class VisitorTrackingService {

  private storageKey = 'anonymousVisitorId';

  constructor(private api: ApiService) {}

  initializeVisitor(): void {

    const existingVisitor =
      localStorage.getItem(this.storageKey);

    if (existingVisitor) {
      return;
    }

    this.api.createVisitor().subscribe({
      next: (response) => {

        localStorage.setItem(
          this.storageKey,
          response.anonymousId
        );

        console.log(
          'Visitor created:',
          response.anonymousId
        );
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
}