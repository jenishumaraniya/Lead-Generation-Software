import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';
import { AIService } from '../../../../../core/services/ai.service';
import { ApiService } from '../../../../../core/services/api.service';
import { Product } from '../../../../../core/models/product.model';

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
  leadProducts: Product[] = [];
  newActivity = { activityType: 'CALL', description: '' };
  loading = true;
  saveMessage = '';
  followUpDateString = '';

  // AI properties
  analysis: any = null;
  aiLoading = false;
  aiError = '';
  analysisExists = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private leadService: LeadService,
    private aiService: AIService,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    if (!id) {
      this.router.navigate(['/sales/leads']);
      return;
    }
    this.loadLead(id);
    this.loadExistingAnalysis(id);
  }

  loadLead(id: number): void {
    this.loading = true;
    this.leadService.getLead(id).subscribe({
      next: (data) => {
        this.lead = data;
        this.salesNote = '';
        if (this.lead) this.lead.notes = '';
        this.activities = data.activities || [];
        this.statusHistories = data.statusHistories || [];
        this.scoreHistories = data.scoreHistories || [];

        const pids = data.productIds || [];
        if (pids.length > 0) {
          this.apiService.getProducts().subscribe({
            next: (prods) => {
              this.leadProducts = (prods || []).filter(p => pids.includes(p.productId));
            }
          });
        } else {
          this.leadProducts = [];
        }

        if (data.nextFollowUpDate) {
          const d = new Date(data.nextFollowUpDate);
          this.followUpDateString = d.toISOString().split('T')[0];
        } else {
          this.followUpDateString = '';
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/sales/leads']);
      }
    });
  }

  getProductQty(productId: number): number {
    if (!this.lead?.productQuantities) return 1;
    const val = this.lead.productQuantities[productId.toString()] || this.lead.productQuantities[productId as any];
    return val ? Number(val) : 1;
  }

  loadExistingAnalysis(leadId: number): void {
    this.aiService.getAnalysis(leadId).subscribe({
      next: (data) => {
        this.analysis = data;
        this.analysisExists = true;
      },
      error: () => {
        this.analysisExists = false;
        this.analysis = null;
      }
    });
  }

  triggerAnalysis(): void {
    if (!this.lead || this.aiLoading) return;
    this.aiLoading = true;
    this.aiError = '';
    this.aiService.analyzeLead(this.lead.leadId).subscribe({
      next: (result) => {
        this.analysis = result;
        this.analysisExists = true;
        this.aiLoading = false;
        this.loadLead(this.lead.leadId);
      },
      error: (err) => {
        this.aiError = err.error?.error || 'AI analysis failed. Please try again.';
        this.aiLoading = false;
      }
    });
  }

  onFollowUpDateChange(val: string): void {
    this.followUpDateString = val;
    if (this.lead) {
      this.lead.nextFollowUpDate = val ? new Date(val).toISOString() : null;
    }
  }

  salesNote: string = '';

  updateLead(): void {
    if (!this.lead) return;
    const noteText = this.salesNote.trim() || (this.lead.notes?.trim() || '');
    this.leadService.updateLead(this.lead.leadId, {
      status: this.lead.status,
      qualification: this.lead.qualification,
      nextFollowUpDate: this.followUpDateString ? new Date(this.followUpDateString).toISOString() : null,
      notes: noteText || undefined
    }).subscribe({
      next: () => {
        this.saveMessage = 'Lead updated successfully';
        setTimeout(() => this.saveMessage = '', 3000);
        this.salesNote = '';
        if (this.lead) this.lead.notes = '';
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
