import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['./audit-logs.component.css']
})
export class AuditLogsComponent implements OnInit {
  logs: any[] = [];
  searchTerm = '';
  isLoading = false;

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.adminApi.getAuditLogs(100, this.searchTerm).subscribe({
      next: (data) => {
        this.logs = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  onSearch(): void {
    this.loadLogs();
  }
}
