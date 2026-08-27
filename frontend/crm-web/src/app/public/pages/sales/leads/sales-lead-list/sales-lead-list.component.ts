import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';
import { AuthService } from '../../../../../core/services/auth.service';

@Component({
  selector: 'app-sales-lead-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './sales-lead-list.component.html',
  styleUrls: ['./sales-lead-list.component.css']
})
export class SalesLeadListComponent implements OnInit {
  leads: Lead[] = [];
  filteredLeads: Lead[] = [];
  statusFilter = '';
  searchTerm = '';
  loading = false;

  constructor(
    private leadService: LeadService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadLeads();
  }

  loadLeads(): void {
    this.loading = true;
    const user = this.authService.getCurrentUser();
    const userId = user?.userId || user?.employeeId;

    this.leadService.getLeads(userId).subscribe({
      next: (data) => {
        if (userId) {
          this.leads = (data || []).filter(l => l.assignedTo === userId);
        } else {
          this.leads = data || [];
        }
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load leads', err);
        this.leads = [];
        this.filteredLeads = [];
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    this.filteredLeads = this.leads.filter(lead => {
      const statusMatch = !this.statusFilter || lead.status === this.statusFilter;
      const searchMatch = !this.searchTerm ||
        (lead.fullName && lead.fullName.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (lead.companyName && lead.companyName.toLowerCase().includes(this.searchTerm.toLowerCase())) ||
        (lead.email && lead.email.toLowerCase().includes(this.searchTerm.toLowerCase()));
      return statusMatch && searchMatch;
    });
  }

  updateLeadStatus(lead: Lead, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const newStatus = select.value;
    if (!newStatus) return;

    this.leadService.updateLead(lead.leadId, { status: newStatus }).subscribe({
      next: () => {
        lead.status = newStatus;
        this.applyFilters();
      },
      error: () => alert('Failed to update lead status')
    });
  }
}
