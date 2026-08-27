import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';

@Component({
  selector: 'app-sales-lead-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './lead-detail.component.html',
  styleUrls: ['./lead-detail.component.css']
})
export class LeadDetailComponent implements OnInit {
  lead: any = null;
  activities: any[] = [];
  statusHistories: any[] = [];
  scoreHistories: any[] = [];
  newActivity = { activityType: 'CALL', description: '' };
  loading = false;
  saveMessage = '';
  followUpDateString = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private leadService: LeadService
  ) {}

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    if (!id) {
      this.router.navigate(['/sales/leads']);
      return;
    }
    this.loadLead(id);
  }

  loadLead(id: number): void {
    this.loading = true;
    this.leadService.getLead(id).subscribe({
      next: (data) => {
        this.lead = data;
        this.activities = data.activities || [];
        this.statusHistories = data.statusHistories || [];
        this.scoreHistories = data.scoreHistories || [];

        if (data.nextFollowUpDate) {
          const d = new Date(data.nextFollowUpDate);
          this.followUpDateString = d.toISOString().split('T')[0];
        } else {
          this.followUpDateString = '';
        }
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load lead', err);
        this.router.navigate(['/sales/leads']);
      }
    });
  }

  onFollowUpDateChange(val: string): void {
    this.followUpDateString = val;
    if (this.lead) {
      this.lead.nextFollowUpDate = val ? new Date(val).toISOString() : null;
    }
  }

  updateLead(): void {
    if (!this.lead) return;
    this.leadService.updateLead(this.lead.leadId, {
      status: this.lead.status,
      qualification: this.lead.qualification,
      nextFollowUpDate: this.followUpDateString ? new Date(this.followUpDateString).toISOString() : null,
      notes: this.lead.notes
    }).subscribe({
      next: () => {
        this.saveMessage = 'Lead updated successfully';
        setTimeout(() => this.saveMessage = '', 3000);
        this.loadLead(this.lead.leadId);
      },
      error: () => alert('Failed to update lead')
    });
  }

  addActivity(): void {
    if (!this.lead || !this.newActivity.description.trim()) return;
    this.leadService.addActivity(this.lead.leadId, this.newActivity).subscribe({
      next: () => {
        this.newActivity.description = '';
        this.loadLead(this.lead.leadId);
      },
      error: () => alert('Failed to log activity')
    });
  }

  goBack(): void {
    this.router.navigate(['/sales/leads']);
  }
}
