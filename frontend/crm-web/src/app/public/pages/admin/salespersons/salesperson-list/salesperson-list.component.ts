import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { Lead, LeadService } from '../../../../../core/services/lead.service';

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

  constructor(private leadService: LeadService) {}

  ngOnInit() {
    this.leadService.getLeads().subscribe({
      next: (data) => {
        this.leads = data;
        this.applyFilters();
      },
      error: () => this.leads = []
    });
  }

  applyFilters() {
    this.filteredLeads = this.leads.filter(lead => {
      const statusMatch = !this.statusFilter || lead.status === this.statusFilter;
      const searchMatch = !this.searchTerm ||
        lead.fullName.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        lead.companyName.toLowerCase().includes(this.searchTerm.toLowerCase());
      return statusMatch && searchMatch;
    });
  }
}