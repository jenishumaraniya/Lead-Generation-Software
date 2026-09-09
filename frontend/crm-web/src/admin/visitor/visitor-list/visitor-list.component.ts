import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminApiService } from '../../../app/core/services/admin-api.services';

@Component({
  selector: 'app-visitor-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './visitor-list.component.html',
  styleUrls: ['./visitor-list.component.css']
})
export class VisitorListComponent implements OnInit {
  visitors: any[] = [];
  isLoading = false;

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadVisitors();
  }

  loadVisitors(): void {
    this.isLoading = true;
    this.adminApi.getAuditLogs(100).subscribe({
      next: (data: any[]) => {
        this.visitors = data || [];
        this.isLoading = false;
      },
      error: () => {
        this.visitors = [];
        this.isLoading = false;
      }
    });
  }
}
