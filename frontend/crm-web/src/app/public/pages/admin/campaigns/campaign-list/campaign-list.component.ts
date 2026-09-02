import { Component, OnInit, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
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
export class CampaignListComponent implements OnInit, AfterViewInit {
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
  pageSize = 10;

  get paginatedCampaigns(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredCampaigns.slice(start, start + this.pageSize);
  }

  stats = {
    total: 0,
    active: 0,
    draft: 0,
    completed: 0,
    totalProspects: 0
  };

  private isSyncingScroll = false;

  constructor(
    private campaignService: CampaignService,
    private sidebarService: SidebarService
  ) {}

  ngOnInit() {
    this.loadCampaigns();
  }

  ngAfterViewInit() {
    this.setupScrollSync();
  }

  loadCampaigns() {
    this.loading = true;
    this.campaignService.getCampaigns().subscribe({
      next: (data) => {
        this.campaigns = data || [];
        this.calculateStats();
        this.applyFilter();
        this.loading = false;
        setTimeout(() => this.setupScrollSync(), 100);
      },
      error: () => {
        this.campaigns = [];
        this.filteredCampaigns = [];
        this.calculateStats();
        this.loading = false;
      }
    });
  }

  calculateStats() {
    this.stats.total = this.campaigns.length;
    this.stats.active = this.campaigns.filter(c => (c.status || '').toUpperCase() === 'ACTIVE').length;
    this.stats.draft = this.campaigns.filter(c => (c.status || '').toUpperCase() === 'DRAFT').length;
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
        const cStatus = (c.status || '').toUpperCase();
        if (cStatus !== this.selectedStatus) return false;
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