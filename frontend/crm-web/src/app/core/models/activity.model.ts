export type ActivityType =
  | 'PRODUCT_VIEW'
  | 'FEATURE_VIEW'
  | 'PRICING_VIEW'
  | 'PRODUCT_COMPARE'
  | 'INTEREST_CLICK';

export interface VisitorActivity {
  activityId: number;
  visitorId: number;
  activityType: ActivityType;
  productId?: number;
  pageUrl?: string;
  metadata?: string;
  timestamp: string;
}