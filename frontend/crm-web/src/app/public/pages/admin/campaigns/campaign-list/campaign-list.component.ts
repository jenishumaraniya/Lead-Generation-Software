import { Component, OnInit, OnDestroy, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CampaignService } from '../../../../../core/services/campaign.service';
import { SidebarService } from '../../../../../core/services/sidebar.service';
import { PaginationComponent } from '../../../../../components/pagination/pagination.component';

@Component({
  selector: 'app-campaign-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PaginationComponent],
  templateUrl: './campaign-list.component.html',
  styleUrls: ['./campaign-list.component.css']
})
export class CampaignListComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('topScroll') topScrollRef!: ElementRef<HTMLDivElement>;
  @ViewChild('tableScroll') tableScrollRef!: ElementRef<HTMLDivElement>;

  campaigns: any[] = [];
  filteredCampaigns: any[] = [];
  loading = false;

  // View & Filters
  viewMode: 'cards' | 'table' = 'cards';
  searchTerm: string = '';
  selectedStatus: string = 'ALL';

  sortColumn: string = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';

  currentPage = 1;
  pageSize = 6;

  get paginatedCampaigns(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredCampaigns.slice(start, start + this.pageSize);
  }

  stats = {
    total: 0,
    active: 0,
    future: 0,
    draft: 0,
    completed: 0,
    expired: 0,
    totalProspects: 0
  };

  private isSyncingScroll = false;

  constructor(
    private campaignService: CampaignService,
    private sidebarService: SidebarService
  ) { }

  private refreshInterval: any;

  ngOnInit() {
    this.loadCampaigns();
    // Auto-refresh campaigns every 5 seconds so status changes dynamically in real time
    this.refreshInterval = setInterval(() => {
      this.loadCampaigns(true);
    }, 5000);
  }

  ngOnDestroy() {
    if (this.refreshInterval) {
      clearInterval(this.refreshInterval);
    }
  }

  private parseDate(d: any): Date {
    if (!d) return new Date(0);
    if (typeof d === 'string' && !d.endsWith('Z') && !d.includes('+')) {
      return new Date(d + 'Z');
    }
    return new Date(d);
  }

  formatDate(dateVal: any): string {
    if (!dateVal) return '—';
    const d = this.parseDate(dateVal);
    if (isNaN(d.getTime()) || d.getTime() === 0) return '—';
    return d.toLocaleString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      hour12: true
    });
  }

  isExpired(campaign: any): boolean {
    if ((campaign.status || '').toUpperCase() === 'EXPIRED') return true;
    if (campaign.scheduleEndDate) {
      return this.parseDate(campaign.scheduleEndDate).getTime() < new Date().getTime();
    }
    return false;
  }

  isFuture(campaign: any): boolean {
    if (this.isExpired(campaign)) return false;
    const status = (campaign.status || '').toUpperCase();
    if (campaign.scheduleStartDate) {
      const isStartInFuture = this.parseDate(campaign.scheduleStartDate).getTime() > new Date().getTime();
      if (status === 'FUTURE') return isStartInFuture;
      if (status === 'ACTIVE') return isStartInFuture;
    }
    return status === 'FUTURE';
  }

  getEffectiveStatus(campaign: any): string {
    if (this.isExpired(campaign)) return 'EXPIRED';
    if (this.isFuture(campaign)) return 'FUTURE';
    const status = (campaign.status || 'DRAFT').toUpperCase();
    // If it was marked FUTURE in DB but start date has arrived and not expired, it is dynamically ACTIVE
    if (status === 'FUTURE' && campaign.scheduleStartDate) {
      if (this.parseDate(campaign.scheduleStartDate).getTime() <= new Date().getTime()) {
        return 'ACTIVE';
      }
    }
    return status;
  }

  ngAfterViewInit() {
    this.setupScrollSync();
  }

  loadCampaigns(silent: boolean = false) {
    if (!silent) this.loading = true;
    this.campaignService.getCampaigns().subscribe({
      next: (data) => {
        this.campaigns = data || [];
        this.calculateStats();
        this.applyFilter();
        if (!silent) this.loading = false;
        setTimeout(() => this.setupScrollSync(), 100);
      },
      error: () => {
        if (!silent) {
          this.campaigns = [];
          this.filteredCampaigns = [];
          this.calculateStats();
          this.loading = false;
        }
      }
    });
  }

  calculateStats() {
    this.stats.total = this.campaigns.length;
    this.stats.active = this.campaigns.filter(c => this.getEffectiveStatus(c) === 'ACTIVE').length;
    this.stats.future = this.campaigns.filter(c => this.getEffectiveStatus(c) === 'FUTURE').length;
    this.stats.draft = this.campaigns.filter(c => (c.status || '').toUpperCase() === 'DRAFT').length;
    this.stats.expired = this.campaigns.filter(c => this.isExpired(c)).length;
    this.stats.completed = this.campaigns.filter(c => ['COMPLETED', 'CLOSED'].includes((c.status || '').toUpperCase())).length;
    this.stats.totalProspects = this.campaigns.reduce((acc, c) => acc + (c.recipientsCount || 0), 0);
  }

  setViewMode(mode: 'cards' | 'table') {
    this.viewMode = mode;
    this.currentPage = 1;
    if (mode === 'table') {
      this.sidebarService.setCollapsed(true);
      setTimeout(() => this.setupScrollSync(), 150);
    } else {
      this.sidebarService.setCollapsed(false);
    }
  }

  applyFilter() {
    this.currentPage = 1;
    const term = this.searchTerm.trim().toLowerCase();

    let list = this.campaigns.filter(c => {
      // Status filter
      if (this.selectedStatus !== 'ALL') {
        const cStatus = this.getEffectiveStatus(c);
        if (this.selectedStatus === 'EXPIRED') {
          if (cStatus !== 'EXPIRED') return false;
        } else if (cStatus !== this.selectedStatus) {
          return false;
        }
      }

      // Search term
      if (!term) return true;

      const nameMatch = c.name?.toLowerCase().includes(term);
      const descMatch = c.description?.toLowerCase().includes(term);
      return nameMatch || descMatch;
    });

    if (this.sortColumn) {
      list.sort((a: any, b: any) => {
        let valA = a[this.sortColumn];
        let valB = b[this.sortColumn];

        if (valA == null) valA = '';
        if (valB == null) valB = '';

        if (typeof valA === 'string') valA = valA.toLowerCase();
        if (typeof valB === 'string') valB = valB.toLowerCase();

        let comparison = 0;
        if (valA > valB) comparison = 1;
        else if (valA < valB) comparison = -1;

        return this.sortDirection === 'asc' ? comparison : -comparison;
      });
    }

    this.filteredCampaigns = list;
    setTimeout(() => this.setupScrollSync(), 100);
  }

  clearSearch() {
    this.searchTerm = '';
    this.applyFilter();
  }

  toggleSort(column: string) {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applyFilter();
  }

  setupScrollSync() {
    if (!this.topScrollRef || !this.tableScrollRef) return;
    const topEl = this.topScrollRef.nativeElement;
    const tableEl = this.tableScrollRef.nativeElement;

    topEl.onscroll = () => {
      if (!this.isSyncingScroll) {
        this.isSyncingScroll = true;
        tableEl.scrollLeft = topEl.scrollLeft;
        setTimeout(() => this.isSyncingScroll = false, 20);
      }
    };

    tableEl.onscroll = () => {
      if (!this.isSyncingScroll) {
        this.isSyncingScroll = true;
        topEl.scrollLeft = tableEl.scrollLeft;
        setTimeout(() => this.isSyncingScroll = false, 20);
      }
    };
  }

  pauseCampaign(id: number) {
    this.campaignService.pauseCampaign(id).subscribe({
      next: () => this.loadCampaigns(),
      error: () => alert('Failed to pause campaign')
    });
  }

  resumeCampaign(id: number) {
    this.campaignService.resumeCampaign(id).subscribe({
      next: () => this.loadCampaigns(),
      error: () => alert('Failed to resume campaign')
    });
  }

  closeCampaign(id: number) {
    if (confirm('Close this campaign?')) {
      this.campaignService.closeCampaign(id).subscribe({
        next: () => this.loadCampaigns(),
        error: () => alert('Failed to close campaign')
      });
    }
  }

  deleteCampaign(id: number) {
    if (confirm('Are you sure you want to delete this campaign?')) {
      this.campaignService.deleteCampaign(id).subscribe({
        next: () => this.loadCampaigns(),
        error: () => alert('Failed to delete campaign')
      });
    }
  }
}