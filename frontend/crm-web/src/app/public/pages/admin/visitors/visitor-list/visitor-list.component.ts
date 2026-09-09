import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AdminApiService } from '../../../../../core/services/admin-api.services';

@Component({
  selector: 'app-visitor-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './visitor-list.component.html',
  styleUrls: ['./visitor-list.component.css']
})
export class VisitorListComponent implements OnInit {
  visitors: any[] = [];
  stats: any = {
    totalVisitors: 0,
    activeToday: 0,
    totalActivities: 0,
    interestClicks: 0,
    convertedCount: 0,
    conversionRate: 0
  };

  loading = true;
  refreshing = false;
  selectedVisitor: any = null;
  drawerLoading = false;
  isDrawerOpen = false;
  searchQuery = '';
  statusFilter = 'ALL';
  copySuccessId: string | null = null;
  errorMessage = '';

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.errorMessage = '';

    this.adminApi.getVisitorStats().subscribe({
      next: (data) => {
        if (data) {
          this.stats = data;
        }
      },
      error: (err) => console.error('Failed to load visitor stats:', err)
    });

    this.adminApi.getVisitors().subscribe({
      next: (data) => {
        this.visitors = data || [];
        this.loading = false;
        this.refreshing = false;
      },
      error: (err) => {
        console.error('Failed to load visitors:', err);
        this.errorMessage = 'Failed to load visitor tracking data. Please ensure the backend is active.';
        this.loading = false;
        this.refreshing = false;
      }
    });
  }

  refresh(): void {
    this.refreshing = true;
    this.loadData();
  }

  get filteredVisitors(): any[] {
    return this.visitors.filter((v) => {
      // Search text filter
      const q = this.searchQuery.trim().toLowerCase();
      let matchesSearch = true;
      if (q) {
        const anonId = (v.anonymousId || '').toLowerCase();
        const leadName = (v.lead?.fullName || '').toLowerCase();
        const leadEmail = (v.lead?.email || '').toLowerCase();
        const leadCompany = (v.lead?.companyName || '').toLowerCase();
        const lastProd = (v.lastProductName || '').toLowerCase();
        const lastAct = (v.lastActivityType || '').toLowerCase();

        matchesSearch = anonId.includes(q) ||
          leadName.includes(q) ||
          leadEmail.includes(q) ||
          leadCompany.includes(q) ||
          lastProd.includes(q) ||
          lastAct.includes(q);
      }

      if (!matchesSearch) return false;

      // Status filter
      if (this.statusFilter === 'CONVERTED') {
        return v.isConverted;
      } else if (this.statusFilter === 'ANONYMOUS') {
        return !v.isConverted;
      } else if (this.statusFilter === 'HIGH_INTENT') {
        return v.interestClicksCount > 0 || v.totalActivities >= 3;
      } else if (this.statusFilter === 'ACTIVE_TODAY') {
        if (!v.lastSeenAt) return false;
        const seenDate = new Date(v.lastSeenAt);
        const today = new Date();
        return seenDate.toDateString() === today.toDateString();
      }

      return true;
    });
  }

  openJourney(visitor: any): void {
    this.isDrawerOpen = true;
    this.drawerLoading = true;
    this.selectedVisitor = visitor;

    this.adminApi.getVisitorDetails(visitor.anonymousId).subscribe({
      next: (data) => {
        this.selectedVisitor = data;
        this.drawerLoading = false;
      },
      error: (err) => {
        console.error('Failed to load visitor journey:', err);
        this.drawerLoading = false;
      }
    });
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
    this.selectedVisitor = null;
  }

  copyAnonymousId(id: string, event: Event): void {
    event.stopPropagation();
    navigator.clipboard.writeText(id).then(() => {
      this.copySuccessId = id;
      setTimeout(() => {
        if (this.copySuccessId === id) this.copySuccessId = null;
      }, 2000);
    });
  }

  timeAgo(dateStr: string | null): string {
    if (!dateStr) return '—';
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  }

  formatDate(dateStr: string | null): string {
    if (!dateStr) return '—';
    const d = new Date(dateStr);
    return d.toLocaleString([], {
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  formatActivityLabel(type: string): string {
    switch (type) {
      case 'INTEREST_CLICK': return 'Interest Clicked';
      case 'PRODUCT_VIEW': return 'Viewed Product';
      case 'LEAD_SUBMITTED': return 'Submitted Lead';
      case 'FORM_SUBMIT': return 'Form Inquiry';
      case 'PAGE_VIEW': return 'Page View';
      case 'COOKIE_CONSENT_ACCEPTED': return 'Consent Given';
      default: return type ? type.replace(/_/g, ' ') : 'Session Active';
    }
  }

  getActivityBadgeClass(type: string): string {
    switch (type) {
      case 'INTEREST_CLICK': return 'badge-interest';
      case 'LEAD_SUBMITTED':
      case 'FORM_SUBMIT': return 'badge-lead';
      case 'PRODUCT_VIEW': return 'badge-product';
      case 'COOKIE_CONSENT_ACCEPTED': return 'badge-consent';
      default: return 'badge-default';
    }
  }

  getIdenticonColor(id: string): string {
    if (!id) return '#64748b';
    const colors = [
      '#3b82f6', '#10b981', '#8b5cf6', '#f59e0b',
      '#ec4899', '#06b6d4', '#6366f1', '#14b8a6'
    ];
    let hash = 0;
    for (let i = 0; i < id.length; i++) {
      hash = id.charCodeAt(i) + ((hash << 5) - hash);
    }
    const idx = Math.abs(hash) % colors.length;
    return colors[idx];
  }

  getInitials(id: string): string {
    if (!id) return 'V';
    return id.substring(0, 2).toUpperCase();
  }
}
