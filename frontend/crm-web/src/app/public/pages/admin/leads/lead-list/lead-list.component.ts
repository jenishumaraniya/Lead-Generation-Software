import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';
import { EmployeeService, Salesperson } from '../../../../../core/services/employee.service';
import { PaginationComponent } from '../../../../../components/pagination/pagination.component';

@Component({
  selector: 'app-lead-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, PaginationComponent],
  templateUrl: './lead-list.component.html',
  styleUrl: './lead-list.component.css'
})
export class LeadListComponent implements OnInit {
  leads: Lead[] = [];
  filteredLeads: Lead[] = [];
  employees: Salesperson[] = [];
  statusFilter = '';
  assignmentFilter = '';
  salespersonFilter: number | '' = '';
  searchTerm = '';
  successToast = '';
  loading = false;

  sortColumn: string = 'createdAt';
  sortDirection: 'asc' | 'desc' = 'desc';

  currentPage = 1;
  pageSize = 10;

  get paginatedLeads(): Lead[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredLeads.slice(start, start + this.pageSize);
  }

  constructor(
    private leadService: LeadService,
    private employeeService: EmployeeService
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.employeeService.getEmployees().subscribe({
      next: (data) => {
        this.employees = (data || []).filter(u => u.role === 'SALES_REP');
        this.loadLeads();
      },
      error: () => {
        this.employees = [];
        this.loadLeads();
      }
    });
  }

  loadLeads(): void {
    this.leadService.getLeads().subscribe({
      next: (data) => {
        this.leads = data || [];
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load leads', err);
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    let list = this.leads.filter(lead => {
      const statusMatch = !this.statusFilter || lead.status === this.statusFilter;
      
      let assignmentMatch = true;
      if (this.assignmentFilter === 'UNASSIGNED') {
        assignmentMatch = !lead.assignedTo;
      } else if (this.assignmentFilter === 'MULTI_CATEGORY') {
        assignmentMatch = !!lead.isMultiCategory;
      } else if (this.assignmentFilter === 'AUTO_ASSIGNED') {
        assignmentMatch = !lead.isMultiCategory && !!lead.assignedTo;
      } else if (this.assignmentFilter === 'ASSIGNED') {
        assignmentMatch = !!lead.assignedTo;
      }

      let salespersonMatch = true;
      if (this.salespersonFilter !== '') {
        salespersonMatch = lead.assignedTo === this.salespersonFilter;
      }

      const searchMatch = !this.searchTerm ||
        (lead.fullName && lead.fullName.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (lead.companyName && lead.companyName.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (lead.email && lead.email.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (lead.assignedSalespersonName && lead.assignedSalespersonName.toLowerCase().includes(this.searchTerm.toLowerCase()));

      return statusMatch && assignmentMatch && salespersonMatch && searchMatch;
    });

    // Apply sorting
    if (this.sortColumn) {
      list.sort((a: any, b: any) => {
        let valA = a[this.sortColumn];
        let valB = b[this.sortColumn];

        if (valA == null) valA = '';
        if (valB == null) valB = '';

        if (typeof valA === 'string') valA = valA.toLowerCase();
        if (typeof valB === 'string') valB = valB.toLowerCase();

        let comparison = 0;
        if (valA > valB) comparison = 1;
        else if (valA < valB) comparison = -1;

        return this.sortDirection === 'asc' ? comparison : -comparison;
      });
    }

    this.filteredLeads = list;
  }

  toggleSort(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
    this.applyFilters();
  }

  assignLead(lead: Lead, employeeId: number | null): void {
    this.leadService.assignLead(lead.leadId, employeeId).subscribe({
      next: (res) => {
        lead.assignedTo = employeeId;
        const emp = this.employees.find(e => e.userId === employeeId);
        lead.assignedSalespersonName = emp ? emp.fullName : undefined;
        lead.assignedCategoryName = emp?.categoryName || undefined;
        this.showToast(res.message || 'Lead assigned successfully by Admin.');
        this.loadLeads();
      },
      error: () => {
        alert('Assignment failed.');
        this.loadLeads();
      }
    });
  }

  private showToast(msg: string): void {
    this.successToast = msg;
    setTimeout(() => this.successToast = '', 3500);
  }
}
