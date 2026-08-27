import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../core/services/lead.service';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-sales-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class SalesDashboardComponent implements OnInit {
  leads: any[] = [];
  stats = {
    total: 0,
    newLeads: 0,
    contacted: 0,
    qualified: 0,
    won: 0
  };
  loading = false;

  constructor(
    private leadService: LeadService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;
    const user = this.authService.getCurrentUser();
    const userId = user?.userId || user?.employeeId;

    this.leadService.getLeads(userId).subscribe({
      next: (data: Lead[]) => {
        if (userId) {
          this.leads = (data || []).filter(l => l.assignedTo === userId);
        } else {
          this.leads = data || [];
        }
        this.computeStats();
        this.loading = false;
      },
      error: () => {
        this.leads = [];
        this.loading = false;
      }
    });
  }

  computeStats(): void {
    this.stats.total = this.leads.length;
    this.stats.newLeads = this.leads.filter(l => l.status === 'NEW').length;
    this.stats.contacted = this.leads.filter(l => l.status === 'CONTACTED').length;
    this.stats.qualified = this.leads.filter(l => l.status === 'QUALIFIED').length;
    this.stats.won = this.leads.filter(l => l.status === 'WON').length;
  }
}