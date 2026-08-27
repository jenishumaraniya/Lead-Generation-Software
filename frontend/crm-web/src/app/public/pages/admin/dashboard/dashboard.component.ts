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

  recentLeads: Lead[] = [];
  loading = false;

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
        this.recentLeads = leadList.slice(0, 6);
        this.loading = false;
      },
      error: () => {
        this.stats.totalLeads = 0;
        this.stats.newLeads = 0;
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
}