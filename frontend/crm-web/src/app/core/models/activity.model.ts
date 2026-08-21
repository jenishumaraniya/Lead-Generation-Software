// export type ActivityType =
//   | 'PRODUCT_VIEW'
//   | 'FEATURE_VIEW'
//   | 'PRICING_VIEW'
//   | 'PRODUCT_COMPARE'
//   | 'INTEREST_CLICK';

// export type ActivityType =
//   | 'PAGE_VIEW'          // ← add this
//   | 'PRODUCT_VIEW'
//   | 'FEATURE_VIEW'
//   | 'PRICING_VIEW'
//   | 'PRODUCT_COMPARE'
//   | 'INTEREST_CLICK';

// export interface VisitorActivity {
//   activityId: number;
//   visitorId: number;
//   activityType: ActivityType;
//   productId?: number;
//   pageUrl?: string;
//   metadata?: string;
//   timestamp: string;
// }

export type ActivityType =
  | 'PAGE_VIEW'
  | 'PRODUCT_VIEW'
  | 'FEATURE_VIEW'
  | 'PRICING_VIEW'
  | 'PRODUCT_COMPARE'
  | 'INTEREST_CLICK'
  | 'LEAD_SUBMITTED'  // ✅ Add this
  | 'cookie_consent_accepted';

export interface VisitorActivity {
  activityId: number;
  visitorId: number;
  activityType: ActivityType;
  productId?: number;
  pageUrl?: string;
  metadata?: string;
  timestamp: string;
}