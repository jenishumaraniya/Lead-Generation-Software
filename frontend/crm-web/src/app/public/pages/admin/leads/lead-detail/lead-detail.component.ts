import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';
import {
  EmployeeService,
  Salesperson,
} from '../../../../../core/services/employee.service';
import { AIService } from '../../../../../core/services/ai.service';
import { ApiService } from '../../../../../core/services/api.service';
import { Product } from '../../../../../core/models/product.model';

@Component({
  selector: 'app-lead-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './lead-detail.component.html',
  styleUrls: ['./lead-detail.component.css'],
})
export class LeadDetailComponent implements OnInit {
  lead: Lead | null = null;
  loading = true;
  activities: any[] = [];
  statusHistories: any[] = [];
  scoreHistories: any[] = [];
  employees: Salesperson[] = [];
  leadProducts: Product[] = [];
  newActivity = { activityType: 'CALL', description: '' };
  isAdmin = false;
  saveToast = '';
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
    private employeeService: EmployeeService,
    private aiService: AIService,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    const id = +this.route.snapshot.params['id'];
    if (!id) {
      this.router.navigate(['/admin/leads']);
      return;
    }
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    this.isAdmin = user?.role === 'ADMIN';

    this.loadLead(id);
    this.loadEmployees();
    this.loadExistingAnalysis(id);
  }

  loadLead(id: number): void {
    this.loading = true;
    this.leadService.getLead(id).subscribe({
      next: (data) => {
        this.lead = data;
        this.adminNote = '';
        if (this.lead) this.lead.notes = '';
        this.loading = false;
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
      },
      error: () => {
        this.loading = false;
        this.router.navigate(['/admin/leads']);
      },
    });
  }

  getProductQty(productId: number): number {
    if (!this.lead?.productQuantities) return 1;
    const val = this.lead.productQuantities[productId.toString()] || this.lead.productQuantities[productId as any];
    return val ? Number(val) : 1;
  }

  loadEmployees(): void {
    this.employeeService.getEmployees().subscribe({
      next: (data) =>
        (this.employees = data.filter((u) => u.role === 'SALES_REP')),
      error: () => (this.employees = []),
    });
  }

  // 👇 Load existing AI analysis if any
  loadExistingAnalysis(leadId: number): void {
    this.aiService.getAnalysis(leadId).subscribe({
      next: (data) => {
        this.analysis = data;
        this.analysisExists = true;
      },
      error: () => {
        this.analysisExists = false;
        this.analysis = null;
      },
    });
  }

  // 👇 Trigger new AI analysis
  triggerAnalysis(): void {
    if (!this.lead || this.aiLoading) return;
    this.aiLoading = true;
    this.aiError = '';
    this.aiService.analyzeLead(this.lead.leadId).subscribe({
      next: (result) => {
        this.analysis = result;
        this.analysisExists = true;
        this.aiLoading = false;
        // Reload lead to see updated qualification/priority
        this.loadLead(this.lead!.leadId);
      },
      error: (err) => {
        this.aiError =
          err.error?.error || 'AI analysis failed. Please try again.';
        this.aiLoading = false;
      },
    });
  }

  onFollowUpDateChange(val: string): void {
    this.followUpDateString = val;
    if (this.lead) {
      this.lead.nextFollowUpDate = val ? new Date(val).toISOString() : null;
    }
  }

  adminNote: string = '';

  saveLead(): void {
    if (!this.lead) return;

    const noteText = this.adminNote.trim() || (this.lead.notes?.trim() || '');

    this.leadService
      .updateLead(this.lead.leadId, {
        status: this.lead.status,
        qualification: this.lead.qualification,
        score: this.lead.score,
        assignedTo: this.lead.assignedTo,
        nextFollowUpDate: this.followUpDateString
          ? new Date(this.followUpDateString).toISOString()
          : null,
        notes: noteText || undefined,
      })
      .subscribe({
        next: () => {
          this.showToast('Lead details and follow-up saved successfully.');
          this.adminNote = '';
          if (this.lead) this.lead.notes = '';
          this.loadLead(this.lead!.leadId);
        },
        error: (err) =>
          alert(err.error?.error || 'Failed to save lead updates.'),
      });
  }

  onAssignChange(targetEmpId: number | null): void {
    if (!this.lead) return;

    this.leadService.assignLead(this.lead.leadId, targetEmpId).subscribe({
      next: (res) => {
        this.lead!.assignedTo = targetEmpId;
        const emp = this.employees.find((e) => e.userId === targetEmpId);
        this.lead!.assignedSalespersonName = emp ? emp.fullName : undefined;
        this.lead!.assignedCategoryName = emp?.categoryName || undefined;
        this.showToast(res.message || 'Lead assigned.');
        this.loadLead(this.lead!.leadId);
      },
      error: () => alert('Assignment failed.'),
    });
  }

  addActivity(): void {
    if (!this.lead || !this.newActivity.description.trim()) return;

    this.leadService.addActivity(this.lead.leadId, this.newActivity).subscribe({
      next: () => {
        this.newActivity = { activityType: 'CALL', description: '' };
        this.showToast('Activity touchpoint logged.');
        this.loadLead(this.lead!.leadId);
      },
      error: () => alert('Failed to log activity touchpoint.'),
    });
  }

  private showToast(msg: string): void {
    this.saveToast = msg;
    setTimeout(() => (this.saveToast = ''), 3500);
  }

  goBack(): void {
    this.router.navigate(['/admin/leads']);
  }
}
