import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';
import { EmployeeService, Salesperson } from '../../../../../core/services/employee.service';

@Component({
  selector: 'app-lead-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './lead-detail.component.html',
  styleUrls: ['./lead-detail.component.css']
})
export class LeadDetailComponent implements OnInit {
  lead: Lead | null = null;
  activities: any[] = [];
  statusHistories: any[] = [];
  scoreHistories: any[] = [];
  employees: Salesperson[] = [];
  newActivity = { activityType: 'CALL', description: '' };
  isAdmin = false;
  saveToast = '';
  followUpDateString = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private leadService: LeadService,
    private employeeService: EmployeeService
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
  }

  loadLead(id: number): void {
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
      },
      error: () => this.router.navigate(['/admin/leads'])
    });
  }

  loadEmployees(): void {
    this.employeeService.getEmployees().subscribe({
      next: (data) => this.employees = data.filter(u => u.role === 'SALES_REP'),
      error: () => this.employees = []
    });
  }

  onFollowUpDateChange(val: string): void {
    this.followUpDateString = val;
    if (this.lead) {
      this.lead.nextFollowUpDate = val ? new Date(val).toISOString() : null;
    }
  }

  saveLead(): void {
    if (!this.lead) return;

    this.leadService.updateLead(this.lead.leadId, {
      status: this.lead.status,
      qualification: this.lead.qualification,
      score: this.lead.score,
      assignedTo: this.lead.assignedTo,
      nextFollowUpDate: this.followUpDateString ? new Date(this.followUpDateString).toISOString() : null,
      notes: this.lead.notes
    }).subscribe({
      next: () => {
        this.showToast('Lead details and follow-up saved successfully.');
        this.loadLead(this.lead!.leadId);
      },
      error: (err) => alert(err.error?.error || 'Failed to save lead updates.')
    });
  }

  onAssignChange(targetEmpId: number | null): void {
    if (!this.lead) return;

    this.leadService.assignLead(this.lead.leadId, targetEmpId).subscribe({
      next: (res) => {
        this.lead!.assignedTo = targetEmpId;
        const emp = this.employees.find(e => e.userId === targetEmpId);
        this.lead!.assignedSalespersonName = emp ? emp.fullName : undefined;
        this.lead!.assignedCategoryName = emp?.categoryName || undefined;
        this.showToast(res.message || 'Lead assigned.');
        this.loadLead(this.lead!.leadId);
      },
      error: () => alert('Assignment failed.')
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
      error: () => alert('Failed to log activity touchpoint.')
    });
  }

  private showToast(msg: string): void {
    this.saveToast = msg;
    setTimeout(() => this.saveToast = '', 3500);
  }

  goBack(): void {
    this.router.navigate(['/admin/leads']);
  }
}