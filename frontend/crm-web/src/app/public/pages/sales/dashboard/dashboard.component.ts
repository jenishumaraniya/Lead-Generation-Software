import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-sales-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class SalesDashboardComponent implements OnInit {
  myLeads = [
    { name: 'ABC Corp', status: 'NEW', nextFollowUp: '2024-08-30' },
    { name: 'XYZ Ltd', status: 'CONTACTED', nextFollowUp: '2024-09-02' }
  ];

  recentActivities = [
    { type: '📞 Call', detail: 'Spoke with John about pricing', time: '2 hours ago' },
    { type: '✉️ Email', detail: 'Sent proposal to Jane', time: '1 day ago' }
  ];

  ngOnInit() {
    // TODO: Fetch real data from API
  }
}