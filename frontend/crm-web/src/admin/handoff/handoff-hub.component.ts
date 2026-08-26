import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminApiService } from '../../app/core/services/admin-api.services';

@Component({
  selector: 'app-handoff-hub',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './handoff-hub.component.html',
  styleUrls: ['./handoff-hub.component.css']
})
export class HandoffHubComponent implements OnInit {
  handoffLogs: any[] = [];
  isLoading = false;
  selectedPayload: string | null = null;
  selectedLeadName: string = '';

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading = true;
    this.adminApi.getHandoffLogs().subscribe({
      next: (data) => {
        this.handoffLogs = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  viewPayload(log: any): void {
    this.selectedLeadName = log.leadName;
    this.selectedPayload = log.payloadJson;
  }

  closePayloadModal(): void {
    this.selectedPayload = null;
  }

  retryHandoff(log: any, event: Event): void {
    event.stopPropagation();
    this.adminApi.retryHandoff(log.leadHandoffId).subscribe({
      next: () => {
        this.loadLogs();
        alert(`Handoff retried successfully for ${log.leadName}`);
      },
      error: (err) => alert(err.error?.error || 'Retry failed')
    });
  }
}
