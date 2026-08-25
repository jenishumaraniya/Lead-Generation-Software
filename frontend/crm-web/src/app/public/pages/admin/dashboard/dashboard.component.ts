import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  stats = [
    { label: 'Total Leads', value: 0, icon: '👥' },
    { label: 'New Leads', value: 0, icon: '🆕' },
    { label: 'Active Campaigns', value: 0, icon: '📢' },
    { label: 'Sales Team', value: 0, icon: '👤' }
  ];

  recentLeads: any[] = [];

  ngOnInit() {
    // TODO: Fetch real data from API
    // For now, show placeholder
    this.stats = [
      { label: 'Total Leads', value: 124, icon: '👥' },
      { label: 'New Leads', value: 18, icon: '🆕' },
      { label: 'Active Campaigns', value: 5, icon: '📢' },
      { label: 'Sales Team', value: 8, icon: '👤' }
    ];
    this.recentLeads = [
      { name: 'Acme Corp', email: 'john@acme.com', status: 'NEW' },
      { name: 'TechStart', email: 'jane@techstart.com', status: 'CONTACTED' }
    ];
  }
}