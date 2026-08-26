import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminApiService } from '../../app/core/services/admin-api.services';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  isLoading = true;
  stats = {
    totalVisitors: 0,
    totalProspects: 0,
    activeCampaigns: 0,
    totalLeads: 0,
    mqlCount: 0,
    sqlCount: 0,
    handoffCount: 0
  };

  recentLeads: any[] = [];
  recentProspects: any[] = [];

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.isLoading = true;

    // Load Visitors
    this.adminApi.getVisitors().subscribe({
      next: (visitors) => {
        this.stats.totalVisitors = visitors.length;
      },
      error: () => {}
    });

    // Load Prospects
    this.adminApi.getProspects().subscribe({
      next: (prospects) => {
        this.stats.totalProspects = prospects.length;
        this.recentProspects = prospects.slice(0, 5);
      },
      error: () => {}
    });

    // Load Campaigns
    this.adminApi.getCampaigns().subscribe({
      next: (campaigns) => {
        this.stats.activeCampaigns = campaigns.filter(c => c.status === 'ACTIVE' || c.status === 'DRAFT').length;
      },
      error: () => {}
    });

    // Load Leads & Qualifications
    this.adminApi.getLeads().subscribe({
      next: (leads) => {
        this.stats.totalLeads = leads.length;
        this.stats.mqlCount = leads.filter(l => l.qualification === 'MQL').length;
        this.stats.sqlCount = leads.filter(l => l.qualification === 'SQL' || l.qualification === 'HOT').length;
        this.recentLeads = leads.slice(0, 5);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      }
    });

    // Load Handoffs
    this.adminApi.getHandoffLogs().subscribe({
      next: (handoffs) => {
        this.stats.handoffCount = handoffs.filter(h => h.status === 'SUCCESS').length;
      },
      error: () => {}
    });
  }
}
