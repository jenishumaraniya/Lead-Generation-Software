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
  allLeads: any[] = [];
  leads: any[] = [];
  stats = {
    total: 0,
    newLeads: 0,
    contacted: 0,
    qualified: 0,
    won: 0
  };
  loading = false;

  sortColumn: string = 'createdAt';
  sortDirection: 'asc' | 'desc' = 'desc';

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
          this.allLeads = (data || []).filter(l => l.assignedTo === userId);
        } else {
          this.allLeads = data || [];
        }
        this.computeStats();
        this.applySort();
        this.loading = false;
      },
      error: () => {
        this.allLeads = [];
        this.leads = [];
        this.loading = false;
      }
    });
  }

  computeStats(): void {
    this.stats.total = this.allLeads.length;
    this.stats.newLeads = this.allLeads.filter(l => l.status === 'NEW').length;
    this.stats.contacted = this.allLeads.filter(l => l.status === 'CONTACTED').length;
    this.stats.qualified = this.allLeads.filter(l => l.status === 'QUALIFIED').length;
    this.stats.won = this.allLeads.filter(l => l.status === 'WON').length;
  }

  applySort(): void {
    let list = [...this.allLeads];
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
    this.leads = list;
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