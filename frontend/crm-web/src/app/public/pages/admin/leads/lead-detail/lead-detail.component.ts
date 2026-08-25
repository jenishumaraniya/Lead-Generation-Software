import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';
import { EmployeeService } from '../../../../../core/services/employee.service';

@Component({
  selector: 'app-lead-detail',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './lead-detail.component.html',
  styleUrls: ['./lead-detail.component.css']
})
export class LeadDetailComponent implements OnInit {
  lead: Lead | null = null;
  activities: any[] = [];
  employees: any[] = [];
  newActivity = { activityType: 'NOTE', description: '' }; // ✅ Changed
  isAdmin = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private leadService: LeadService,
    private employeeService: EmployeeService
  ) {}

  ngOnInit() {
    const id = +this.route.snapshot.params['id'];
    if (!id) return;
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    this.isAdmin = user?.role === 'ADMIN';
    this.loadLead(id);
    this.loadActivities(id);
    if (this.isAdmin) this.loadEmployees();
  }

  loadLead(id: number) {
    this.leadService.getLead(id).subscribe({
      next: (data) => this.lead = data,
      error: () => this.router.navigate(['/admin/leads'])
    });
  }

  loadActivities(id: number) {
    this.leadService.getActivities(id).subscribe({
      next: (data) => this.activities = data,
      error: () => this.activities = []
    });
  }

  loadEmployees() {
    this.employeeService.getEmployees().subscribe({
      next: (data) => this.employees = data,
      error: () => this.employees = []
    });
  }

  updateLead() {
    if (!this.lead) return;
    this.leadService.updateLead(this.lead.leadId, {
      status: this.lead.status,
      assignedTo: this.lead.assignedTo,
      nextFollowUpDate: this.lead.nextFollowUpDate,
      notes: this.lead.notes
    }).subscribe({
      next: () => console.log('Lead updated'),
      error: (err) => alert('Update failed')
    });
  }

  assignLead() {
    if (!this.lead || this.lead.assignedTo === null) return;
    this.leadService.assignLead(this.lead.leadId, this.lead.assignedTo).subscribe({
      next: () => console.log('Assigned'),
      error: () => alert('Assignment failed')
    });
  }

  addActivity() {
    if (!this.lead || !this.newActivity.description) return;
    this.leadService.addActivity(this.lead.leadId, this.newActivity).subscribe({
      next: () => {
        this.newActivity = { activityType: 'NOTE', description: '' };
        this.loadActivities(this.lead!.leadId);
      },
      error: () => alert('Failed to add activity')
    });
  }

  goBack() {
    this.router.navigate(['/admin/leads']);
  }
}