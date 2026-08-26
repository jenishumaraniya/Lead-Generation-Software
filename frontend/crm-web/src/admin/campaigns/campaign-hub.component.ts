import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../app/core/services/admin-api.services';

@Component({
  selector: 'app-campaign-hub',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './campaign-hub.component.html',
  styleUrls: ['./campaign-hub.component.css']
})
export class CampaignHubComponent implements OnInit {
  campaigns: any[] = [];
  prospects: any[] = [];
  selectedCampaign: any = null;
  isLoading = false;

  showCreateModal = false;
  showEnrollModal = false;
  selectedCampaignIdToEnroll: number | null = null;
  selectedProspectIdToEnroll: number | null = null;

  newCampaign = {
    name: 'Enterprise Outreach Q1',
    description: 'Automated 3-step sequence targeting VP Sales & CROs',
    status: 'ACTIVE',
    steps: [
      { stepNumber: 1, name: 'Initial Introduction', subject: 'Strategic collaboration for {{Company}}', body: 'Hi {{Name}},\n\nI noticed your leadership in revenue operations at {{Company}}. We help high-growth teams accelerate qualified lead generation.\n\nBest,\nSales Team', delayDays: 0, delayHours: 0 },
      { stepNumber: 2, name: 'Value Proposition & Case Study', subject: 'How similar enterprise teams scaled lead velocity', body: 'Hi {{Name}},\n\nFollowing up on my previous note. We recently helped similar enterprise companies increase SQL pipeline by 40%.\n\nWould you be open to a 10-minute overview?', delayDays: 2, delayHours: 0 },
      { stepNumber: 3, name: 'Final Breakup Check-in', subject: 'Closing the loop on lead generation for {{Company}}', body: 'Hi {{Name}},\n\nI realize you are busy. If scaling qualified inbound & outbound leads is not a priority right now, I will follow up later.\n\nBest regards.', delayDays: 4, delayHours: 0 }
    ]
  };

  constructor(private adminApi: AdminApiService) {}

  ngOnInit(): void {
    this.loadCampaigns();
    this.loadProspects();
  }

  loadCampaigns(): void {
    this.isLoading = true;
    this.adminApi.getCampaigns().subscribe({
      next: (data) => {
        this.campaigns = data;
        if (data.length > 0 && !this.selectedCampaign) {
          this.viewCampaign(data[0].campaignId);
        }
        this.isLoading = false;
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  loadProspects(): void {
    this.adminApi.getProspects().subscribe({
      next: (data) => {
        this.prospects = data;
      },
      error: () => {}
    });
  }

  viewCampaign(campaignId: number): void {
    this.adminApi.getCampaign(campaignId).subscribe({
      next: (campaign) => {
        this.selectedCampaign = campaign;
      },
      error: (err) => console.error(err)
    });
  }

  openCreateModal(): void {
    this.showCreateModal = true;
  }

  closeCreateModal(): void {
    this.showCreateModal = false;
  }

  addStep(): void {
    const stepNo = this.newCampaign.steps.length + 1;
    this.newCampaign.steps.push({
      stepNumber: stepNo,
      name: `Follow-up Step ${stepNo}`,
      subject: `Quick follow-up for {{Name}}`,
      body: `Hi {{Name}},\n\nChecking in regarding lead generation at {{Company}}.`,
      delayDays: 3,
      delayHours: 0
    });
  }

  removeStep(index: number): void {
    if (this.newCampaign.steps.length > 1) {
      this.newCampaign.steps.splice(index, 1);
    }
  }

  createCampaign(): void {
    this.adminApi.createCampaign(this.newCampaign).subscribe({
      next: () => {
        this.closeCreateModal();
        this.loadCampaigns();
      },
      error: (err) => console.error(err)
    });
  }

  togglePause(campaign: any, event: Event): void {
    event.stopPropagation();
    if (campaign.status === 'ACTIVE') {
      this.adminApi.pauseCampaign(campaign.campaignId).subscribe({
        next: () => this.loadCampaigns()
      });
    } else {
      this.adminApi.resumeCampaign(campaign.campaignId).subscribe({
        next: () => this.loadCampaigns()
      });
    }
  }

  openEnrollModal(campaignId: number, event: Event): void {
    event.stopPropagation();
    this.selectedCampaignIdToEnroll = campaignId;
    this.showEnrollModal = true;
  }

  closeEnrollModal(): void {
    this.showEnrollModal = false;
    this.selectedCampaignIdToEnroll = null;
    this.selectedProspectIdToEnroll = null;
  }

  enrollProspect(): void {
    if (!this.selectedCampaignIdToEnroll || !this.selectedProspectIdToEnroll) return;

    this.adminApi.enrollProspectInCampaign(this.selectedCampaignIdToEnroll, this.selectedProspectIdToEnroll).subscribe({
      next: () => {
        this.closeEnrollModal();
        if (this.selectedCampaign) {
          this.viewCampaign(this.selectedCampaign.campaignId);
        }
      },
      error: (err) => {
        alert(err.error?.error || 'Failed to enroll prospect (may already be enrolled or suppressed).');
      }
    });
  }
}
