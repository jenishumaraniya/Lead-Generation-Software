import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../core/services/lead.service';
import { CampaignService } from '../../../../core/services/campaign.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { CategoryService } from '../../../../core/services/category.service';

export interface ChartSegment {
  label: string;
  count: number;
  percentage: number;
  color: string;
  dashArray: string;
  dashOffset: number;
}

export interface MonthlyDataPoint {
  month: string;
  count: number;
  barHeightPercent: number;
}

export interface CategoryDataPoint {
  categoryName: string;
  count: number;
  percentage: number;
  color: string;
}

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

  // Timeframe Filter
  selectedTimeframe: '24H' | 'WEEK' | 'MONTH' | 'ALL' = 'MONTH';

  // Chart Data (Derived 100% from Live Database Records)
  qualSegments: ChartSegment[] = [];
  monthlyTrend: MonthlyDataPoint[] = [];
  categoryDistribution: CategoryDataPoint[] = [];
  statusDistribution: { label: string; count: number; color: string; percentage: number }[] = [];

  activeBarHover: MonthlyDataPoint | null = null;

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

    // 1. Fetch live Leads from API
    this.leadService.getLeads().subscribe({
      next: (leads) => {
        const leadList = leads || [];
        this.stats.totalLeads = leadList.length;
        this.stats.newLeads = leadList.filter(l => (l.status || '').toUpperCase() === 'NEW').length;
        this.allRecentLeads = leadList;
        this.applySort();
        this.updateDashboardForTimeframe();
        this.loading = false;
      },
      error: () => {
        this.stats.totalLeads = 0;
        this.stats.newLeads = 0;
        this.allRecentLeads = [];
        this.recentLeads = [];
        this.calculateCharts([]);
        this.loading = false;
      }
    });

    // 2. Fetch live Campaigns from API
    this.campaignService.getCampaigns().subscribe({
      next: (campaigns) => {
        const campaignList = campaigns || [];
        this.stats.activeCampaigns = campaignList.filter(c => (c.status || '').toUpperCase() === 'ACTIVE').length;
      },
      error: () => {
        this.stats.activeCampaigns = 0;
      }
    });

    // 3. Fetch live Sales Team from API
    this.employeeService.getEmployees().subscribe({
      next: (users) => {
        const userList = users || [];
        this.stats.salesTeamCount = userList.filter(u => u.role === 'SALES_REP').length;
      },
      error: () => {
        this.stats.salesTeamCount = 0;
      }
    });

    // 4. Fetch live Categories from API
    this.categoryService.getCategories().subscribe({
      next: (categories) => {
        this.stats.categoriesCount = categories?.length || 0;
      },
      error: () => {
        this.stats.categoriesCount = 0;
      }
    });
  }

  setTimeframe(tf: '24H' | 'WEEK' | 'MONTH' | 'ALL'): void {
    this.selectedTimeframe = tf;
    this.updateDashboardForTimeframe();
  }

  private updateDashboardForTimeframe(): void {
    const filteredLeads = this.getLeadsForTimeframe(this.allRecentLeads);
    this.calculateCharts(filteredLeads);
  }

  private getLeadsForTimeframe(leads: Lead[]): Lead[] {
    if (!leads || leads.length === 0) return [];
    if (this.selectedTimeframe === 'ALL') return leads;

    const now = new Date().getTime();
    const oneDay = 24 * 60 * 60 * 1000;

    return leads.filter(l => {
      if (!l.createdAt) return true;
      const created = new Date(l.createdAt).getTime();
      const diffDays = (now - created) / oneDay;

      if (this.selectedTimeframe === '24H') return diffDays <= 1;
      if (this.selectedTimeframe === 'WEEK') return diffDays <= 7;
      if (this.selectedTimeframe === 'MONTH') return diffDays <= 30;
      return true;
    });
  }

  private calculateCharts(leads: Lead[]): void {
    const total = leads.length;

    // 1. Qualification Segments (HOT, WARM, COLD, MQL) calculated strictly from real database leads
    const hotCount = leads.filter(l => (l.score || 0) >= 60 && (l.score || 0) < 90 || (l.qualification || '').toUpperCase() === 'HOT').length;
    const warmCount = leads.filter(l => (l.score || 0) >= 30 && (l.score || 0) < 60 || (l.qualification || '').toUpperCase() === 'WARM').length;
    const coldCount = leads.filter(l => (l.score || 0) < 30 && ((l.qualification || '').toUpperCase() === 'COLD' || !l.qualification)).length;
    const mqlCount = leads.filter(l => (l.score || 0) >= 90 || (l.qualification || '').toUpperCase() === 'MQL').length;

    const rawSegments = [
      { label: 'Hot Leads', count: hotCount, color: '#ef4444' },       // Red
      { label: 'Warm Leads', count: warmCount, color: '#f59e0b' },     // Amber
      { label: 'Cold Leads', count: coldCount, color: '#3b82f6' },     // Blue
      { label: 'MQL (High-Intent)', count: mqlCount, color: '#8b5cf6' }  // Purple
    ];

    const circumference = 314.159; // 2 * PI * 50
    let cumulative = 0;

    this.qualSegments = rawSegments.map(s => {
      const pct = total > 0 ? Math.round((s.count / total) * 100) : 0;
      const segmentLen = total > 0 ? (s.count / total) * circumference : 0;
      const strokeDashArray = `${segmentLen} ${circumference - segmentLen}`;
      const strokeDashOffset = -cumulative;
      cumulative += segmentLen;

      return {
        label: s.label,
        count: s.count,
        percentage: pct,
        color: s.color,
        dashArray: strokeDashArray,
        dashOffset: strokeDashOffset
      };
    });

    // 2. Real Date Bucketing for Trend Bar Chart
    let labels: string[] = [];
    let counts: number[] = [];

    if (this.selectedTimeframe === '24H') {
      labels = ['04:00', '08:00', '12:00', '16:00', '20:00', '24:00'];
      counts = [0, 0, 0, 0, 0, 0];
      leads.forEach(l => {
        if (!l.createdAt) return;
        const hr = new Date(l.createdAt).getHours();
        const idx = Math.min(Math.floor(hr / 4), 5);
        counts[idx]++;
      });
    } else if (this.selectedTimeframe === 'WEEK') {
      labels = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
      counts = [0, 0, 0, 0, 0, 0, 0];
      leads.forEach(l => {
        if (!l.createdAt) return;
        const day = new Date(l.createdAt).getDay();
        counts[day]++;
      });
    } else if (this.selectedTimeframe === 'MONTH') {
      labels = ['Wk 1', 'Wk 2', 'Wk 3', 'Wk 4'];
      counts = [0, 0, 0, 0];
      leads.forEach(l => {
        if (!l.createdAt) return;
        const date = new Date(l.createdAt).getDate();
        const idx = Math.min(Math.floor((date - 1) / 7), 3);
        counts[idx]++;
      });
    } else {
      labels = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
      counts = new Array(12).fill(0);
      leads.forEach(l => {
        if (!l.createdAt) return;
        const m = new Date(l.createdAt).getMonth();
        counts[m]++;
      });
      // Slice to active months up to current month
      const currentMonthIdx = new Date().getMonth();
      labels = labels.slice(0, currentMonthIdx + 1);
      counts = counts.slice(0, currentMonthIdx + 1);
    }

    const maxCount = Math.max(...counts, 1);

    this.monthlyTrend = labels.map((lbl, idx) => ({
      month: lbl,
      count: counts[idx],
      barHeightPercent: Math.round((counts[idx] / maxCount) * 100)
    }));

    // 3. Category Distribution (Filter out zero-count categories)
    const catMap: { [key: string]: number } = {};
    leads.forEach(l => {
      const cat = l.assignedCategoryName || l.domain || 'General Inquiries';
      catMap[cat] = (catMap[cat] || 0) + 1;
    });

    const catTotal = Object.values(catMap).reduce((a, b) => a + b, 0) || 1;
    const categoryColors = ['#2563eb', '#10b981', '#8b5cf6', '#f59e0b', '#ec4899', '#06b6d4'];
    let colorIdx = 0;

    this.categoryDistribution = Object.keys(catMap)
      .map(catName => {
        const cnt = catMap[catName];
        const color = categoryColors[colorIdx % categoryColors.length];
        colorIdx++;
        return {
          categoryName: catName,
          count: cnt,
          percentage: total > 0 ? Math.round((cnt / catTotal) * 100) : 0,
          color: color
        };
      })
      .filter(cat => cat.count > 0);

    // 4. Status Breakdown (Filter out zero-count statuses)
    const statusMap = [
      { label: 'New Inbound', status: 'NEW', color: '#2563eb' },
      { label: 'Contacted', status: 'CONTACTED', color: '#f59e0b' },
      { label: 'Qualified', status: 'QUALIFIED', color: '#10b981' },
      { label: 'Converted (Won)', status: 'WON', color: '#059669' },
      { label: 'Lost / Closed', status: 'LOST', color: '#ef4444' }
    ];

    this.statusDistribution = statusMap
      .map(st => {
        const cnt = leads.filter(l => (l.status || '').toUpperCase() === st.status).length;
        return {
          label: st.label,
          count: cnt,
          color: st.color,
          percentage: total > 0 ? Math.round((cnt / total) * 100) : 0
        };
      })
      .filter(st => st.count > 0);
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