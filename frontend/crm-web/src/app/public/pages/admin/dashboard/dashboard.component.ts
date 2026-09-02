import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../core/services/lead.service';
import { CampaignService } from '../../../../core/services/campaign.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { CategoryService } from '../../../../core/services/category.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  stats = {
    totalLeads: 0,
    newLeads: 0,
    activeCampaigns: 0,
    salesTeamCount: 0,
    categoriesCount: 0
  };

  allRecentLeads: Lead[] = [];
  recentLeads: Lead[] = [];
  loading = false;

  sortColumn: string = 'createdAt';
  sortDirection: 'asc' | 'desc' = 'desc';

  constructor(
    private leadService: LeadService,
    private campaignService: CampaignService,
    private employeeService: EmployeeService,
    private categoryService: CategoryService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;

    // 1. Fetch live Leads
    this.leadService.getLeads().subscribe({
      next: (leads) => {
        const leadList = leads || [];
        this.stats.totalLeads = leadList.length;
        this.stats.newLeads = leadList.filter(l => l.status === 'NEW').length;
        this.allRecentLeads = leadList;
        this.applySort();
        this.loading = false;
      },
      error: () => {
        this.stats.totalLeads = 0;
        this.stats.newLeads = 0;
        this.allRecentLeads = [];
        this.recentLeads = [];
        this.loading = false;
      }
    });

    // 2. Fetch live Campaigns
    this.campaignService.getCampaigns().subscribe({
      next: (campaigns) => {
        const campaignList = campaigns || [];
        this.stats.activeCampaigns = campaignList.filter(c => c.status === 'ACTIVE').length;
      },
      error: () => {
        this.stats.activeCampaigns = 0;
      }
    });

    // 3. Fetch live Sales Team
    this.employeeService.getEmployees().subscribe({
      next: (users) => {
        const userList = users || [];
        this.stats.salesTeamCount = userList.filter(u => u.role === 'SALES_REP').length;
      },
      error: () => {
        this.stats.salesTeamCount = 0;
      }
    });

    // 4. Fetch live Categories
    this.categoryService.getCategories().subscribe({
      next: (categories) => {
        this.stats.categoriesCount = categories?.length || 0;
      },
      error: () => {
        this.stats.categoriesCount = 0;
      }
    });
  }

  applySort(): void {
    let list = [...this.allRecentLeads];
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
    this.recentLeads = list.slice(0, 6);
  }

  toggleSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applySort();
  }
}