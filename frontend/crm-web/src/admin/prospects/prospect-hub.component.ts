import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';

@Component({
  selector: 'app-prospect-hub',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './prospect-hub.component.html',
  styleUrls: ['./prospect-hub.component.css']
})
export class ProspectHubComponent implements OnInit {
  prospects: any[] = [];
  filteredProspects: any[] = [];
  searchTerm = '';
  statusFilter = 'ALL';
  isLoading = false;
  isDiscovering = false;
  enrichingProspectId: number | null = null;

  showDiscoveryModal = false;
  discoveryCriteria = {
    jobTitle: 'VP of Sales',
    industry: 'Enterprise Software & SaaS',
    geography: 'United States',
    companySize: '100-500',
    company: '',
    autoEnrich: true
  };

  selectedProspectDetails: any = null;
  showDetailsDrawer = false;

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadProspects();
  }

  loadProspects(): void {
    this.isLoading = true;
    this.adminApi.getProspects().subscribe({
      next: (data) => {
        this.prospects = data;
        this.applyFilter();
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  applyFilter(): void {
    this.filteredProspects = this.prospects.filter(p => {
      const matchesSearch = !this.searchTerm ||
        p.name?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        p.email?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        p.jobTitle?.toLowerCase().includes(this.searchTerm.toLowerCase()) ||
        p.company?.name?.toLowerCase().includes(this.searchTerm.toLowerCase());

      const matchesStatus = this.statusFilter === 'ALL' || p.status === this.statusFilter;
      return matchesSearch && matchesStatus;
    });
  }

  openDiscoveryModal(): void {
    this.showDiscoveryModal = true;
  }

  closeDiscoveryModal(): void {
    this.showDiscoveryModal = false;
  }

  runDiscovery(): void {
    this.isDiscovering = true;
    this.adminApi.discoverProspects(this.discoveryCriteria).subscribe({
      next: (res) => {
        this.isDiscovering = false;
        this.closeDiscoveryModal();
        this.loadProspects();
      },
      error: (err) => {
        console.error(err);
        this.isDiscovering = false;
      }
    });
  }

  triggerEnrichment(prospect: any, event?: Event): void {
    if (event) event.stopPropagation();
    this.enrichingProspectId = prospect.prospectId;

    this.adminApi.triggerLinkedInEnrichment(prospect.prospectId).subscribe({
      next: () => {
        this.enrichingProspectId = null;
        this.loadProspects();
        if (this.selectedProspectDetails && this.selectedProspectDetails.prospectId === prospect.prospectId) {
          this.viewProspectDetails(prospect);
        }
      },
      error: (err) => {
        console.error(err);
        this.enrichingProspectId = null;
      }
    });
  }

  viewProspectDetails(prospect: any): void {
    this.selectedProspectDetails = null;
    this.showDetailsDrawer = true;
    this.adminApi.getProspectEnrichment(prospect.prospectId).subscribe({
      next: (details) => {
        this.selectedProspectDetails = details;
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  closeDrawer(): void {
    this.showDetailsDrawer = false;
    this.selectedProspectDetails = null;
  }
}
