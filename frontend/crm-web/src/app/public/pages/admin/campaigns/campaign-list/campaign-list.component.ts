import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CampaignService } from '../../../../../core/services/campaign.service';

@Component({
  selector: 'app-campaign-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './campaign-list.component.html',
  styleUrls: ['./campaign-list.component.css']
})
export class CampaignListComponent implements OnInit {
  campaigns: any[] = [];

  constructor(private campaignService: CampaignService) {}

  ngOnInit() {
    this.loadCampaigns();
  }

  loadCampaigns() {
    this.campaignService.getCampaigns().subscribe({
      next: (data) => this.campaigns = data,
      error: () => this.campaigns = []
    });
  }

  closeCampaign(id: number) {
    if (confirm('Close this campaign?')) {
      this.campaignService.closeCampaign(id).subscribe({
        next: () => this.loadCampaigns(),
        error: () => alert('Failed to close campaign')
      });
    }
  }
}