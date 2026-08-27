import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CampaignService } from '../../../../../core/services/campaign.service';

@Component({
  selector: 'app-campaign-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule],
  templateUrl: './campaign-form.component.html',
  styleUrls: ['./campaign-form.component.css']
})
export class CampaignFormComponent implements OnInit {
  form: FormGroup;
  isEdit = false;
  campaignId: number | null = null;

  // Prospects & CSV State
  allProspects: any[] = [];
  filteredProspects: any[] = [];
  selectedProspectIds: Set<number> = new Set<number>();
  prospectSearchTerm = '';
  csvUploadMessage = '';
  isUploadingCsv = false;
  saving = false;

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
      status: ['ACTIVE'],
      subject: ['Exploring opportunities with {{Company}}', Validators.required],
      body: ['<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>', Validators.required],
      sendImmediately: [true]
    });
  }

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.isEdit = true;
      this.campaignId = +id;
      this.campaignService.getCampaign(this.campaignId).subscribe({
        next: (data) => {
          const step1 = data.steps && data.steps.length > 0 ? data.steps[0] : null;
          this.form.patchValue({
            name: data.name || '',
            description: data.description || '',
            status: data.status || 'ACTIVE',
            scheduleStartDate: this.toInputDateTime(data.scheduleStartDate),
            scheduleEndDate: this.toInputDateTime(data.scheduleEndDate),
            subject: step1?.subject || 'Exploring opportunities with {{Company}}',
            body: step1?.body || '<p>Hello {{Name}},</p><p>I noticed your work as {{JobTitle}} at {{Company}}. We provide enterprise solutions tailored for your industry.</p><p>Would you be open to a brief introductory conversation this week?</p><p>Best regards,<br/>Sales & Partnerships Team</p>',
            sendImmediately: false
          });
          if (data.recipients && Array.isArray(data.recipients)) {
            data.recipients.forEach((r: any) => this.selectedProspectIds.add(r.prospectId));
          }
        },
        error: () => this.router.navigate(['/admin/campaigns'])
      });
    }

    this.loadProspects();
  }

  private toInputDateTime(dateVal: any): string {
    if (!dateVal) return '';
    const d = new Date(dateVal);
    if (isNaN(d.getTime())) return '';
    const pad = (n: number) => n < 10 ? '0' + n : '' + n;
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  loadProspects(): void {
    this.campaignService.getProspects().subscribe({
      next: (data) => {
        this.allProspects = data || [];
        this.applyProspectFilter();
      },
      error: () => {
        this.allProspects = [];
        this.filteredProspects = [];
      }
    });
  }

  applyProspectFilter(): void {
    if (!this.prospectSearchTerm) {
      this.filteredProspects = [...this.allProspects];
      return;
    }
    const term = this.prospectSearchTerm.toLowerCase();
    this.filteredProspects = this.allProspects.filter(p =>
      (p.name && p.name.toLowerCase().includes(term)) ||
      (p.email && p.email.toLowerCase().includes(term)) ||
      (p.jobTitle && p.jobTitle.toLowerCase().includes(term)) ||
      (p.company && p.company.name && p.company.name.toLowerCase().includes(term))
    );
  }

  toggleProspectSelection(prospectId: number): void {
    if (this.selectedProspectIds.has(prospectId)) {
      this.selectedProspectIds.delete(prospectId);
    } else {
      this.selectedProspectIds.add(prospectId);
    }
  }

  toggleSelectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    if (checked) {
      this.filteredProspects.forEach(p => this.selectedProspectIds.add(p.prospectId));
    } else {
      this.filteredProspects.forEach(p => this.selectedProspectIds.delete(p.prospectId));
    }
  }

  isAllSelected(): boolean {
    return this.filteredProspects.length > 0 &&
      this.filteredProspects.every(p => this.selectedProspectIds.has(p.prospectId));
  }

  downloadTemplate(): void {
    window.open(this.campaignService.downloadCsvTemplateUrl(), '_blank');
  }

  onCsvFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    const formData = new FormData();
    formData.append('file', file);

    this.isUploadingCsv = true;
    this.csvUploadMessage = 'Uploading and processing CSV...';

    this.campaignService.uploadProspectsCsv(formData).subscribe({
      next: (res) => {
        this.isUploadingCsv = false;
        this.csvUploadMessage = `CSV Processed: ${res.added} added, ${res.updated} updated. Total: ${res.totalProcessed}.`;
        this.loadProspects();
        // Automatically select newly uploaded prospects
        if (res.prospects && Array.isArray(res.prospects)) {
          res.prospects.forEach((p: any) => this.selectedProspectIds.add(p.prospectId));
        }
        input.value = '';
      },
      error: (err) => {
        this.isUploadingCsv = false;
        this.csvUploadMessage = err.error?.error || 'Failed to upload CSV.';
        input.value = '';
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    this.saving = true;
    const formVal = this.form.value;

    const payload = {
      name: formVal.name?.trim(),
      description: formVal.description?.trim() || null,
      status: formVal.status || 'ACTIVE',
      scheduleStartDate: formVal.scheduleStartDate ? new Date(formVal.scheduleStartDate).toISOString() : null,
      scheduleEndDate: formVal.scheduleEndDate ? new Date(formVal.scheduleEndDate).toISOString() : null,
      steps: [
        {
          stepNumber: 1,
          name: 'Initial Outreach',
          subject: formVal.subject?.trim() || `Exploring opportunities - ${formVal.name}`,
          body: formVal.body?.trim() || '<p>Hello {{Name}},</p><p>We wanted to reach out regarding solutions for {{Company}}.</p>',
          delayDays: 0,
          delayHours: 0
        }
      ]
    };

    const obs = this.isEdit
      ? this.campaignService.updateCampaign(this.campaignId!, payload)
      : this.campaignService.createCampaign(payload);

    obs.subscribe({
      next: (campaign) => {
        const id = this.isEdit ? this.campaignId! : campaign.campaignId;
        // Enroll all selected prospects
        if (this.selectedProspectIds.size > 0) {
          const enrollPromises = Array.from(this.selectedProspectIds).map(prospectId =>
            this.campaignService.enrollProspect(id, prospectId).toPromise()
          );
          Promise.allSettled(enrollPromises).then(() => {
            if (formVal.sendImmediately || formVal.status === 'ACTIVE') {
              this.campaignService.launchCampaign(id).subscribe({
                next: () => {
                  this.saving = false;
                  this.router.navigate(['/admin/campaigns']);
                },
                error: () => {
                  this.saving = false;
                  this.router.navigate(['/admin/campaigns']);
                }
              });
            } else {
              this.saving = false;
              this.router.navigate(['/admin/campaigns']);
            }
          });
        } else {
          this.saving = false;
          this.router.navigate(['/admin/campaigns']);
        }
      },
      error: () => {
        this.saving = false;
        alert('Failed to save campaign');
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/admin/campaigns']);
  }
}