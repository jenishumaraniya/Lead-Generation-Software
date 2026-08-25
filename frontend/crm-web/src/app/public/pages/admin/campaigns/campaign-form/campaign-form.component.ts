import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CampaignService } from '../../../../../core/services/campaign.service';

@Component({
  selector: 'app-campaign-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './campaign-form.component.html',
  styleUrls: ['./campaign-form.component.css']
})
export class CampaignFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  campaignId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private campaignService: CampaignService
  ) {
    this.form = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      scheduleStartDate: [''],
      scheduleEndDate: [''],
      status: ['DRAFT']
    });
  }

  ngOnInit() {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.isEdit = true;
      this.campaignId = +id;
      this.campaignService.getCampaign(this.campaignId).subscribe({
        next: (data) => this.form.patchValue(data),
        error: () => this.router.navigate(['/admin/campaigns'])
      });
    }
  }

  onSubmit() {
    if (this.form.invalid) return;
    const data = this.form.value;
    const obs = this.isEdit
      ? this.campaignService.updateCampaign(this.campaignId!, data)
      : this.campaignService.createCampaign(data);
    obs.subscribe({
      next: () => this.router.navigate(['/admin/campaigns']),
      error: () => alert('Failed to save campaign')
    });
  }

  goBack() {
    this.router.navigate(['/admin/campaigns']);
  }
}