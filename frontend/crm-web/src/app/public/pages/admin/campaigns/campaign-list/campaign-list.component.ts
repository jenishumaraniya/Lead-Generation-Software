import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { CampaignService } from '../../../../../core/services/campaign.service';

@Component({
  selector: 'app-campaign-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './campaign-list.component.html',
  styleUrls: ['./campaign-list.component.css']
})
export class CampaignListComponent implements OnInit {
  campaigns: any[] = [];
  filteredCampaigns: any[] = [];
  loading = false;

  // View & Filters
  viewMode: 'cards' | 'table' = 'cards';
  searchTerm: string = '';
  selectedStatus: string = 'ALL';

  stats = {
    total: 0,
    active: 0,
    draft: 0,
    completed: 0,
    totalProspects: 0
  };

  constructor(private campaignService: CampaignService) {}

  ngOnInit() {
    this.loadCampaigns();
  }

  loadCampaigns() {
    this.loading = true;
    this.campaignService.getCampaigns().subscribe({
      next: (data) => {
        this.campaigns = data || [];
        this.calculateStats();
        this.applyFilter();
        this.loading = false;
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
  }

  applyFilter() {
    const term = this.searchTerm.trim().toLowerCase();

    this.filteredCampaigns = this.campaigns.filter(c => {
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