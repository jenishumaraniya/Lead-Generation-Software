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
  newActivity = { activityType: 'CALL', description: '' };
  loading = false;
  saveMessage = '';

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
    this.loadActivities(id);
  }

  loadLead(id: number): void {
    this.loading = true;
    this.leadService.getLead(id).subscribe({
      next: (data) => {
        this.lead = data;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load lead', err);
        this.router.navigate(['/sales/leads']);
      }
    });
  }

  loadActivities(id: number): void {
    this.leadService.getActivities(id).subscribe({
      next: (data) => this.activities = data || [],
      error: () => this.activities = []
    });
  }

  updateLead(): void {
    if (!this.lead) return;
    this.leadService.updateLead(this.lead.leadId, {
      status: this.lead.status,
      qualification: this.lead.qualification,
      notes: this.lead.notes
    }).subscribe({
      next: () => {
        this.saveMessage = 'Lead updated successfully';
        setTimeout(() => this.saveMessage = '', 3000);
      },
      error: () => alert('Failed to update lead')
    });
  }

  addActivity(): void {
    if (!this.lead || !this.newActivity.description.trim()) return;
    this.leadService.addActivity(this.lead.leadId, this.newActivity).subscribe({
      next: () => {
        this.newActivity.description = '';
        this.loadActivities(this.lead.leadId);
      },
      error: () => alert('Failed to log activity')
    });
  }

  goBack(): void {
    this.router.navigate(['/sales/leads']);
  }
}
