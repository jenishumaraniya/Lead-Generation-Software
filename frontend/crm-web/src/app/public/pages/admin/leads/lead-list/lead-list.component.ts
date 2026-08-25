import { Component } from '@angular/core';
import { Lead, LeadService } from '../../../../../core/services/lead.service';
import { EmployeeService } from '../../../../../core/services/employee.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-lead-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './lead-list.component.html',
  styleUrl: './lead-list.component.css'
})
export class LeadListComponent {

  leads: Lead[] = [];
  filteredLeads: Lead[] = [];
  employees: any[] = [];
  statusFilter = '';
  searchTerm = '';

  constructor(
    private leadService: LeadService,
    private employeeService: EmployeeService
  ) {}

  ngOnInit() {
    this.loadLeads();
    this.loadEmployees();
  }

  loadLeads() {
    this.leadService.getLeads().subscribe({
      next: (data) => {
        this.leads = data;
        this.applyFilters();
      },
      error: (err) => console.error('Failed to load leads', err)
    });
  }

  loadEmployees() {
    this.employeeService.getEmployees().subscribe({
      next: (data) => this.employees = data,
      error: () => this.employees = []
    });
  }

  applyFilters() {
    this.filteredLeads = this.leads.filter(lead => {
      const statusMatch = !this.statusFilter || lead.status === this.statusFilter;
      const searchMatch = !this.searchTerm ||
        lead.fullName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        lead.companyName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        lead.email.toLowerCase().includes(this.searchTerm.toLowerCase());
      return statusMatch && searchMatch;
    });
  }

  assignLead(leadId: number, event: Event) {
    const select = event.target as HTMLSelectElement;
    const employeeId = select.value ? parseInt(select.value) : null;
    if (employeeId) {
      this.leadService.assignLead(leadId, employeeId).subscribe({
        next: () => this.loadLeads(),
        error: (err) => alert('Assignment failed')
      });
    }
  }
}
